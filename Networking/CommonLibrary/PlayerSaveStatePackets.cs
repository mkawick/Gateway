using System;
using System.IO;
using Packets;
using Network;

public class PlayerSaveStatePacket : BasePacket
{
    public override PacketType PacketType { get { return PacketType.PlayerSaveState; } }

    public PlayerSaveState state;
    public override void Dispose()
    {
        if (state != null)
        {
            state = null;
        }
    }

    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);
        state.Write(writer);
    }

    public override void Read(BinaryReader reader)
    {
        base.Read(reader);
        // TODO: Non-alloc version of this
        state = new PlayerSaveState();
        state.Read(reader);
    }

    public override void CopyFrom(BasePacket packet)
    {
        base.CopyFrom(packet);
        var typedPacket = (PlayerSaveStatePacket)packet;
        // TODO: Non-alloc version of this
        state = new PlayerSaveState();
        state.CopyFrom(typedPacket.state);
    }
}

public class UpdatePlayerSaveStatePacket : BasePacket
{
    public override PacketType PacketType { get { return PacketType.UpdatePlayerSaveState; } }

    public PlayerSaveStateData state;

    public override void Dispose()
    {
        if( state != null)
        {
            state = null;
        }
    }
    public override void Write(BinaryWriter writer)
    {
        base.Write(writer);
        state.Write(writer);
    }

    public override void Read(BinaryReader reader)
    {
        base.Read(reader);
        // TODO: Non-alloc version of this
        state = new PlayerSaveStateData();
        state.Read(reader);
    }

    public override void CopyFrom(BasePacket packet)
    {
        base.CopyFrom(packet);
        var typedPacket = (UpdatePlayerSaveStatePacket)packet;
        // TODO: Non-alloc version of this
        state = new PlayerSaveStateData();
        state.CopyFrom(typedPacket.state);
    }
}

public class PlayerSaveStateData : IBinarySerializable
{
    public string state = string.Empty;

    public bool HasData()
    {
        return state != null && state.Length > 0;
    }

    public void Write(BinaryWriter writer)
    {
        if (state == null)
        {
            state = string.Empty;
        }
        writer.Write(state);
    }

    public void Read(BinaryReader reader)
    {
        state = reader.ReadString();
    }

    public void CopyFrom(PlayerSaveStateData other)
    {
        state = other.state;
    }

    public override string ToString()
    {
        return state;
    }
}

public class PlayerSaveState : IBinarySerializable
{
    public const int NO_CHARACTER_ID = -1;

    public int accountId;
    public int characterId = NO_CHARACTER_ID;
    // These string fields can't be serialized if they're null
    // so nulls will be converted to empty string before serialization
    public string name = string.Empty;
    public PlayerSaveStateData state = new PlayerSaveStateData();

    public bool hasState()
    {
        return characterId != NO_CHARACTER_ID
            && name != null && name.Length > 0
            && state != null && state.HasData();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(accountId);
        writer.Write(characterId);
        if (name == null)
        {
            name = string.Empty;
        }
        writer.Write(name);
        if (state == null)
        {
            // TODO: Non-alloc version of this
            state = new PlayerSaveStateData();
        }
        state.Write(writer);
    }

    public void Read(BinaryReader reader)
    {
        accountId = reader.ReadInt32();
        characterId = reader.ReadInt32();
        name = reader.ReadString();
        // TODO: Non-alloc version of this
        state = new PlayerSaveStateData();
        state.Read(reader);
    }

    public void CopyFrom(PlayerSaveState other)
    {
        accountId = other.accountId;
        characterId = other.accountId;
        name = other.name;
        // TODO: Non-alloc version of this
        state = new PlayerSaveStateData();
        state.CopyFrom(other.state);
    }

    public override string ToString()
    {
        string result = String.Format("PlayerSaveState: accountId: {0}", accountId);
        if (hasState())
        {
            result += String.Format("characterId: {0}, name: {1}, state:{2}", characterId, name, state);
        }
        else
        {
            result += "No state";
        }
        return result;
    }

}
