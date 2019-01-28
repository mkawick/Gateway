using Packets;
using System;

public class ClientNPC : ClientWorldEntity
{
    public event Action<NPC_BTState> OnBTStateFromServer;
    public event Action<NPC_BlackBoard> OnBBStateFromServer;

    public override void Init(IClientNetworking client, int entityId)
    {
        base.Init(client, entityId);
        client.AddListener<NPC_BlackBoard>(entityId, ReceiveBBPacket);
        client.AddListener<NPC_BTState>(entityId, ReceiveBTPacket);
    }

    private void ReceiveBBPacket(NPC_BlackBoard obj)
    {
        OnBBStateFromServer?.Invoke(obj);
    }

    private void ReceiveBTPacket(NPC_BTState obj)
    {
        OnBTStateFromServer?.Invoke(obj);
    }
}
