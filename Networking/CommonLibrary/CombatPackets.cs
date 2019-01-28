using System;
using System.IO;
using Vectors;

namespace Packets
{
    public class Combat_AttackRequest : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.Combat_AttackRequest; } }

        public int frameId;
        public int abilityId;

        public int targetId;
        public Vector3 position;
        //public Vector3 direction;
       

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            abilityId = reader.ReadInt32();
            targetId = reader.ReadInt32();

            position.Read(reader);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(frameId);
            writer.Write(abilityId);
            writer.Write(targetId);

            position.Write(writer);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_AttackRequest)packet;
            frameId = typedPacket.frameId;
            abilityId = typedPacket.abilityId;
            targetId = typedPacket.targetId;
            position = typedPacket.position;
        }
    }

    public class Combat_AttackOriginate : Combat_AttackRequest
    {
        public override PacketType PacketType { get { return PacketType.Combat_AttackOriginate; } }

        public int attackerId; 
        public int abilityInstanceId;
        public Combat_AttackOriginate() : base(){ }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            attackerId = reader.ReadInt32();
            abilityInstanceId = reader.ReadInt32();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(attackerId);
            writer.Write(abilityInstanceId);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_AttackOriginate)packet;
            attackerId = typedPacket.attackerId;
            abilityInstanceId = typedPacket.abilityInstanceId;
        }
    }

    public class Combat_AttackStop : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.Combat_AttackStop; } }

        public int abilityInstanceId;
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            abilityInstanceId = reader.ReadInt32();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(abilityInstanceId);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_AttackStop)packet;
            abilityInstanceId = typedPacket.abilityInstanceId;
        }
    }

    public class Combat_BuffApply : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.Combat_BuffApply; } }

        const int NumBuffs = 8;
        public int frameId;
        public int[] buffIds = new int[NumBuffs];

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            byte[] buffer = reader.ReadBytes(sizeof(int)* NumBuffs);
            buffIds = Array.ConvertAll(buffer, Convert.ToInt32);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(frameId);
            byte[] buffer  = Array.ConvertAll(buffIds, Convert.ToByte);
            writer.Write(buffer);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_BuffApply)packet;
            frameId = typedPacket.frameId;
            Array.Copy(typedPacket.buffIds, buffIds, buffIds.Length);
        }
    }

    public class Combat_BuffRemove : Combat_BuffApply
    {
        public override PacketType PacketType { get { return PacketType.Combat_BuffRemove; } }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
        }
    }

    public class Combat_HealthChange : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.Combat_HealthChange; } }

        public int frameId;
        public int currentHealthValue;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            currentHealthValue = reader.ReadInt32();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(frameId);
            writer.Write(currentHealthValue);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_HealthChange)packet;
            frameId = typedPacket.frameId;
            currentHealthValue = typedPacket.currentHealthValue;
        }
    }

    public class Combat_StaminaChange : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.Combat_StaminaChange; } }

        public int frameId;
        public int currentStaminaValue;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            currentStaminaValue = reader.ReadInt32();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(frameId);
            writer.Write(currentStaminaValue);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Combat_StaminaChange)packet;
            frameId = typedPacket.frameId;
            currentStaminaValue = typedPacket.currentStaminaValue;
        }
    }

}