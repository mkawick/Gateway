using Packets;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class ServerPlayer : ServerWorldEntity
{
    // Used for retrieving transform info from the world
    private static ServerWorldEntity[] nearbyEntities = new ServerWorldEntity[20];
    // Used for retrieving players that need to be informed about our state changes
    private static ServerPlayer[] nearbyPlayers = new ServerPlayer[20];

    // Entities that we are currently tracking on the client
    private HashSet<int> trackedEntityIds = new HashSet<int>();

    private int nearbyEntitiesCount = 0;

    public int AccountId { get; protected set; }
    
    private PlayerSaveState saveState;

    public event Action<WorldEntityPacket> OnPositionReceived;


    public override void Init(int entityId, IServerNetworking unityServer)
    {
        base.Init(entityId, unityServer);
        unityServer.AddListener<PlayerSaveStatePacket>(entityId, SaveStatePacket);
        unityServer.AddListener<UpdatePlayerSaveStatePacket>(entityId, UpdateSaveStatePacket);
        unityServer.AddListener<Combat_AttackRequest>(entityId, AttackRequestPacket);
        unityServer.AddListener<ProfileCreateCharacterResponse>(entityId, CreateCharacterResponsePacket);
        unityServer.AddListener<WorldEntityPacket>(entityId, WorldEntityPacketReceived);
    }

    void WorldEntityPacketReceived(WorldEntityPacket packet)
    {
        //todo: verify movement
        Position = packet.position.Get();
        Rotation = packet.rotation.Get();
        IsDirty = true;
    }

    private void CreateCharacterResponsePacket(ProfileCreateCharacterResponse packet)
    {
        if (saveState == null)
        {
            Debug.LogFormat("Unable to update player state, as it doesn't exist: entityId: {0}, accountId: {1}", EntityId, AccountId);
            return;
        }

        // This may have a characterId of NO_CHARACTER_ID if the insert failed
        saveState.characterId = packet.characterId;
        return;
    }

    private void UpdateSaveStatePacket(UpdatePlayerSaveStatePacket packet)
    {
        // We pass these create / update requests to the profile server we're connected to
        if (saveState.characterId == PlayerSaveState.NO_CHARACTER_ID)
        {
            ProfileCreateCharacterRequest request = (ProfileCreateCharacterRequest)IntrepidSerialize.TakeFromPool(PacketType.ProfileCreateCharacterRequest);
            request.accountId = saveState.accountId;
            request.productName = "hungry hippos";  //TODO: configure this!
            request.characterName = saveState.name; // TODO: Need to have some kind of create character packet where they can specify name
            request.state = packet.state;
            Server.SendToProfileServer(request);
        }
        else
        {
            ProfileUpdateCharacter request = (ProfileUpdateCharacter)IntrepidSerialize.TakeFromPool(PacketType.ProfileUpdateCharacter);
            request.characterId = saveState.characterId;
            request.state = packet.state;
            Server.SendToProfileServer(request);
        }

        // We assume the state will be created / saved
        saveState.state = packet.state;
    }

    void SaveStatePacket(PlayerSaveStatePacket packet)
    {
        Debug.Log("RECEIVED!");
        trackedEntityIds = new HashSet<int>();
        AccountId = packet.state.accountId;
        this.saveState = packet.state;

        SpatialPartitioning.Register(this);
    }

    private void AttackRequestPacket(Combat_AttackRequest packet)
    {
        // TODO: Validate attack.
        Combat_AttackOriginate ao = (Combat_AttackOriginate)IntrepidSerialize.TakeFromPool(PacketType.Combat_AttackOriginate);
        ao.attackerId = EntityId;
        ao.frameId = Server.FrameID;
        ao.abilityId = packet.abilityId;
        ao.targetId = packet.targetId;
        ao.position = packet.position;

        // keep in mind threading things
        /*int nearbyPlayersCount = */SpatialPartitioning.GetEntitiesInRadius(ref nearbyPlayers, Position);
        Server.Send(ao, nearbyPlayers.Select(e => e.EntityId));
    }

    //TODO: Split this up, as it is doing 2 things:
    //Updating nearby entities
    //Updating the position
    public void UpdateNearbyEntities()
    {
        // TODO: Hysteresis
        // We'll need to have a way to look up an entity by its id
        nearbyEntitiesCount = SpatialPartitioning.GetEntitiesInRadius(ref nearbyEntities, Position);

        for (int i = 0; i < nearbyEntitiesCount; i++)
        {
            var entity = nearbyEntities[i];
            if (entity == this || entity == null) continue;

            // Assume we haven't seen this entity before
            int frameIdLastUpdated = NetworkConstants.BeforeStartOfGameFrameId;
            
            // If we have seen this entity before...
            if (trackedEntityIds.Contains(entity.EntityId))
            {
                // TODO : Store the actual last frame we updated them, but for the moment
                // we update them every frame, so we saw them last frame
                frameIdLastUpdated = Server.FrameID - 1;
            }

            entity.SendState(this, frameIdLastUpdated);

            trackedEntityIds.Remove(entity.EntityId);
        }

        // These have all now disappered
        foreach (var entityId in trackedEntityIds)
        {
            // Tell the client to remove that entity
            EntityDestroyPacket destroyPacket = (EntityDestroyPacket)IntrepidSerialize.TakeFromPool(PacketType.EntityDestroy);
            destroyPacket.entityId = entityId;
            Server.Send(destroyPacket, EntityId.SingleItemAsEnumerable());
        }

        // Create the new list of tracked entities
        trackedEntityIds.Clear();
        for (int i = 0; i < nearbyEntitiesCount; i++)
        {
            var entity = nearbyEntities[i];
            if (entity == this || entity == null) continue;
            trackedEntityIds.Add(entity.EntityId);
        }
    }

    public override void SendInitData(ServerPlayer destPlayer, EntityFullPacket packet = null)
    {
        PlayerFullPacket pfp = packet as PlayerFullPacket;
        if (pfp == null)
        {
            pfp = (PlayerFullPacket)IntrepidSerialize.TakeFromPool(PacketType.PlayerFull);
        }
        base.SendInitData(destPlayer, pfp);
    }

    public override void SendPositionAndRotationData(ServerPlayer destPlayer)
    {
        WorldEntityPacket liveEntityPosition = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
        liveEntityPosition.entityId = EntityId;
        liveEntityPosition.position.Set(Position);
        liveEntityPosition.rotation.Set(Rotation);

        Server.Send(liveEntityPosition, destPlayer.EntityId.SingleItemAsEnumerable());
    }

    public void SendPlayerSaveState(string defaultSaveState, PlayerSaveState stateFromServer)
    {
        PlayerSaveStatePacket packet = (PlayerSaveStatePacket)IntrepidSerialize.TakeFromPool(PacketType.PlayerSaveState);

        saveState = stateFromServer;

        packet.state = saveState;
        if (!saveState.hasState())
        {
            // the player doesn't exist yet
            // so load the default state
            packet.state.name = "Player" + packet.state.accountId;
            packet.state.state.state = defaultSaveState;
        }

        Server.Send(packet, this.EntityId.SingleItemAsEnumerable());
    }
        
    public override void Tick()
    {
        transform.position = Position;
        transform.rotation = Quaternion.Euler(Rotation);

        SpatialPartitioning.Update(this);
    }
}