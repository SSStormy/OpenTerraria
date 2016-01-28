﻿using System;
using System.IO;

namespace TerrariaBridge.Packet
{
    public sealed class WorldInfo : PacketWrapper
    {
        public int Time { get; private set; }
        public byte DayMoonInfo { get; private set; }
        public byte MoonPhase { get; private set; }
        public short MaxTilesX { get; private set; }
        public short MaxTilesY { get; private set; }
        public short SpawnX { get; private set; }
        public short SpawnY { get; private set; }
        public short WorldSurface { get; private set; }
        public short RockLayer { get; private set; }
        public int WorldId { get; private set; }
        public string WorldName { get; private set; }
        public byte MoonType { get; private set; }
        public byte TreeBackground { get; private set; }
        public byte CorruptionBackground { get; private set; }
        public byte JungleBackground { get; private set; }
        public byte SnowBackground { get; private set; }
        public byte HallowBackground { get; private set; }
        public byte CrimsonBackground { get; private set; }
        public byte DesertBackground { get; private set; }
        public byte OceanBackground { get; private set; }
        public byte IceBackStyle { get; private set; }
        public byte JungleBackStyle { get; private set; }
        public byte HellBackStyle { get; private set; }
        public float WindSpeedSet { get; private set; }
        public byte CloudNumber { get; private set; }
        public int Tree1 { get; private set; }
        public int Tree2 { get; private set; }
        public int Tree3 { get; private set; }
        public byte TreeStyle1 { get; private set; }
        public byte TreeStyle2 { get; private set; }
        public byte TreeStyle3 { get; private set; }
        public byte TreeStyle4 { get; private set; }
        public int CaveBack1 { get; private set; }
        public int CaveBack2 { get; private set; }
        public int CaveBack3 { get; private set; }
        public byte CaveBackStyle1 { get; private set; }
        public byte CaveBackStyle2 { get; private set; }
        public byte CaveBackStyle3 { get; private set; }
        public byte CaveBackStyle4 { get; private set; }
        public float Rain { get; private set; }
        public byte EventInfo1 { get; private set; }
        public byte EventInfo2 { get; private set; }
        public byte EventInfo3 { get; private set; }
        public byte EventInfo4 { get; private set; }
        public sbyte InvasionType { get; private set; }
        public ulong LobbyId { get; private set; }

        public WorldInfo() { }

        protected override void WritePayload(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override void ReadPayload(PayloadReader reader, TerrPacketType type)
        {
            if (type != TerrPacketType.WorldInformation)
                throw new ArgumentException($"{nameof(type)} is not {TerrPacketType.WorldInformation}");

            Time = reader.ReadInt32();
            DayMoonInfo = reader.ReadByte();
            MoonPhase = reader.ReadByte();
            MaxTilesX = reader.ReadInt16();
            MaxTilesY = reader.ReadInt16();
            SpawnX = reader.ReadInt16();
            SpawnY = reader.ReadInt16();
            WorldSurface = reader.ReadInt16();
            RockLayer = reader.ReadInt16();
            WorldId = reader.ReadInt32();
            WorldName = reader.ReadString();
            MoonType = reader.ReadByte();
            TreeBackground = reader.ReadByte();
            CorruptionBackground = reader.ReadByte();
            JungleBackground = reader.ReadByte();
            SnowBackground = reader.ReadByte();
            HallowBackground = reader.ReadByte();
            CrimsonBackground = reader.ReadByte();
            DesertBackground = reader.ReadByte();
            OceanBackground = reader.ReadByte();
            IceBackStyle = reader.ReadByte();
            JungleBackStyle = reader.ReadByte();
            HellBackStyle = reader.ReadByte();
            WindSpeedSet = reader.ReadSingle();
            CloudNumber = reader.ReadByte();
            Tree1 = reader.ReadInt32();
            Tree2 = reader.ReadInt32();
            Tree3 = reader.ReadInt32();
            TreeStyle1 = reader.ReadByte();
            TreeStyle2 = reader.ReadByte();
            TreeStyle3 = reader.ReadByte();
            TreeStyle4 = reader.ReadByte();
            CaveBack1 = reader.ReadInt32();
            CaveBack2 = reader.ReadInt32();
            CaveBack3 = reader.ReadInt32();
            CaveBackStyle1 = reader.ReadByte();
            CaveBackStyle2 = reader.ReadByte();
            CaveBackStyle3 = reader.ReadByte();
            CaveBackStyle4 = reader.ReadByte();
            Rain = reader.ReadSingle();
            EventInfo1 = reader.ReadByte();
            EventInfo2 = reader.ReadByte();
            EventInfo3 = reader.ReadByte();
            EventInfo4 = reader.ReadByte();
            InvasionType = reader.ReadSByte();
            LobbyId = reader.ReadUInt64();
        }
    }
}
