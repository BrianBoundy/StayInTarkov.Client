﻿using LiteNetLib.Utils;
using StayInTarkov.Coop.NetworkPacket;
using static StayInTarkov.Networking.SITSerialization;

namespace StayInTarkov.Networking.Packets
{
    public struct CommonPlayerPacket : INetSerializable
    {
        public bool ShouldSend { get; private set; } = false;
        public string ProfileId { get; set; }
        public EPhraseTrigger Phrase { get; set; } = EPhraseTrigger.PhraseNone;
        public int PhraseIndex { get; set; }
        public bool HasWorldInteractionPacket { get; set; }
        public WorldInteractionPacket WorldInteractionPacket { get; set; }
        public bool HasContainerInteractionPacket { get; set; }
        public ContainerInteractionPacket ContainerInteractionPacket { get; set; }
        public bool HasProceedPacket { get; set; }
        public ProceedPacket ProceedPacket { get; set; }
        public bool HasHeadLightsPackage { get; set; }
        public HeadLightsPacket HeadLightsPacket { get; set; }
        public bool HasInventoryChanged { get; set; }
        public bool SetInventoryOpen { get; set; }
        public bool HasDrop { get; set; }
        public DropPacket DropPacket { get; set; }

        public CommonPlayerPacket(string profileId)
        {
            ProfileId = profileId;
            HasWorldInteractionPacket = false;
            HasContainerInteractionPacket = false;
            HasProceedPacket = false;
            HasHeadLightsPackage = false;
            HasInventoryChanged = false;
            HasDrop = false;
        }
        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            Phrase = (EPhraseTrigger)reader.GetInt();
            if (Phrase != EPhraseTrigger.PhraseNone)
                PhraseIndex = reader.GetInt();
            HasWorldInteractionPacket = reader.GetBool();
            if (HasWorldInteractionPacket)
                WorldInteractionPacket = WorldInteractionPacket.Deserialize(reader);
            HasContainerInteractionPacket = reader.GetBool();
            if (HasContainerInteractionPacket)
                ContainerInteractionPacket = ContainerInteractionPacket.Deserialize(reader);
            HasProceedPacket = reader.GetBool();
            if (HasProceedPacket)
                ProceedPacket = ProceedPacket.Deserialize(reader);
            HasHeadLightsPackage = reader.GetBool();
            if (HasHeadLightsPackage)
                HeadLightsPacket = HeadLightsPacket.Deserialize(reader);
            HasInventoryChanged = reader.GetBool();
            if (HasInventoryChanged)
                SetInventoryOpen = reader.GetBool();
            HasDrop = reader.GetBool();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put((int)Phrase);
            if (Phrase != EPhraseTrigger.PhraseNone)
                writer.Put(PhraseIndex);
            writer.Put(HasWorldInteractionPacket);
            if (HasWorldInteractionPacket)
                WorldInteractionPacket.Serialize(writer, WorldInteractionPacket);
            writer.Put(HasContainerInteractionPacket);
            if (HasContainerInteractionPacket)
                ContainerInteractionPacket.Serialize(writer, ContainerInteractionPacket);
            writer.Put(HasProceedPacket);
            if (HasProceedPacket)
                ProceedPacket.Serialize(writer, ProceedPacket);
            writer.Put(HasHeadLightsPackage);
            if (HasHeadLightsPackage)
                HeadLightsPacket.Serialize(writer, HeadLightsPacket);
            writer.Put(HasInventoryChanged);
            if (HasInventoryChanged)
                writer.Put(SetInventoryOpen);
            writer.Put(HasDrop);
        }

        public void ToggleSend()
        {
            if (!ShouldSend)
                ShouldSend = true;
        }
    }
}
