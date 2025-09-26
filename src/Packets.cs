using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeTalker.Packets;
using Newtonsoft.Json;

namespace Atlyss_DPSUI;

public struct DamageHistory {
    public float time;
    public int damage;
}


/// <summary>
/// Struct representing a player in a packet so the player info isn't duplicated for boss and total dungeon damage
/// </summary>
public struct PacketPlayer {

    public uint netId;
    public uint color;
    public string nickname;
    public string icon;

    public PacketPlayer() { } // Prevent json instantiation fuckery

    public PacketPlayer(DPSValues ogValue) {
        netId = ogValue.netId;
        color = ogValue.color;
        nickname = ogValue.nickname;
        icon = ogValue.classIcon;
    }
}

public struct PacketDPSValue {
    public byte playerIndex;
    public uint value;

    public PacketDPSValue() { }

    public PacketDPSValue(int index, uint _value) {
        playerIndex = unchecked((byte)index); // Server should be unusable long before this overflows
        value = _value;
    }
}

public class BinaryDPSPacket : BinaryPacketBase {

    public uint mapNetID;
    public long dungeonStartTime;
    public long dungeonClearTime;
    public long bossTeleportTime;
    public long bossFightStartTime;
    public long bossFightEndTime;
    public List<PacketPlayer> players;
    public List<DPSValues> bossDamageValues;
    public List<DPSValues> partyDamageValues;

    public BinaryDPSPacket() { }

    internal BinaryDPSPacket(DungeonInstance instance) {
        mapNetID = instance.mapNetID;
        dungeonStartTime = instance.dungeonStartTime;
        dungeonClearTime = instance.dungeonClearTime;
        bossTeleportTime = instance.bossTeleportTime;
        bossFightStartTime = instance.bossFightStartTime;
        bossFightEndTime = instance.bossFightEndTime;
        bossDamageValues = instance.bossDamage;
        partyDamageValues = instance.totalDungeonDamage;

        players = new List<PacketPlayer>();


        var dict = new Dictionary<uint, DPSValues>();
        foreach (var p in bossDamageValues.Concat(partyDamageValues))
            if (!dict.ContainsKey(p.netId))
                dict[p.netId] = p;

        var dedupedPlayerList = dict.Values.ToArray();
        for (int i = 0; i < dedupedPlayerList.Length; i++)
            players.Add(new PacketPlayer(dedupedPlayerList[i]));
    }

    public override void Deserialize(byte[] data) {
        var ms = new MemoryStream(data);
        var reader = new BinaryReader(ms);

        mapNetID = reader.ReadUInt32();
        dungeonStartTime = reader.ReadInt64();
        dungeonClearTime = reader.ReadInt64();
        bossTeleportTime = reader.ReadInt64();
        bossFightStartTime = reader.ReadInt64();
        bossFightEndTime = reader.ReadInt64();

        byte playerCount = reader.ReadByte();
        players = new List<PacketPlayer>(playerCount);

        uint netId, damage, color;
        string nickname, icon;
        for (int i = 0; i < playerCount; i++) {
            netId = reader.ReadUInt32();
            color = reader.ReadUInt32();

            List<byte> strBytes = new List<byte>();
            byte c;
            while ((c = reader.ReadByte()) != 0)
                strBytes.Add(c);

            nickname = Encoding.UTF8.GetString(strBytes.ToArray());
            strBytes.Clear();
            while ((c = reader.ReadByte()) != 0)
                strBytes.Add(c);

            icon = Encoding.UTF8.GetString(strBytes.ToArray());

            PacketPlayer p = new PacketPlayer {
                netId = netId,
                color = color,
                nickname = nickname,
                icon = icon
            };
            players.Add(p);
        }


        byte bossDmgCount = reader.ReadByte();
        bossDamageValues = new List<DPSValues>(bossDmgCount);
        Plugin.logger.LogInfo($"Deserializing {bossDmgCount} boss damage values");
        PacketPlayer pPlayer;
        for (int i = 0; i < bossDmgCount; i++) {
            pPlayer = players[reader.ReadByte()];
            damage = reader.ReadUInt32();
            Plugin.logger.LogInfo($"{pPlayer.nickname}: {damage}");
            bossDamageValues.Add(new DPSValues(pPlayer, damage));
        }

        byte partyDmgCount = reader.ReadByte();
        partyDamageValues = new List<DPSValues>(partyDmgCount);
        Plugin.logger.LogInfo($"Deserializing {partyDmgCount} party damage values");
        for (int i = 0; i < partyDmgCount; i++) {
            pPlayer = players[reader.ReadByte()];
            damage = reader.ReadUInt32();
            Plugin.logger.LogInfo($"{pPlayer.nickname}: {damage}");
            partyDamageValues.Add(new DPSValues(pPlayer, damage));
        }
    }

    public override byte[] Serialize() {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);

        writer.Write(mapNetID);
        writer.Write(dungeonStartTime);
        writer.Write(dungeonClearTime);
        writer.Write(bossTeleportTime);
        writer.Write(bossFightStartTime);
        writer.Write(bossFightEndTime);
        writer.Write(unchecked((byte)players.Count));

        foreach (var p in players) {
            writer.Write(p.netId);
            writer.Write(p.color);
            writer.Write(Encoding.UTF8.GetBytes(p.nickname));
            writer.Write((byte)0); // String null terminator
            writer.Write(Encoding.UTF8.GetBytes(p.icon));
            writer.Write((byte)0); // String null terminator
        }

        writer.Write(unchecked((byte)bossDamageValues.Count));
        foreach (var dmg in bossDamageValues) {
            writer.Write(unchecked((byte)players.FindIndex(p => p.netId == dmg.netId)));
            writer.Write(dmg.totalDamage);
        }

        writer.Write(unchecked((byte)partyDamageValues.Count));
        foreach (var dmg in partyDamageValues) {
            writer.Write(unchecked((byte)players.FindIndex(p => p.netId == dmg.netId)));
            writer.Write(dmg.totalDamage);
        }

        return ms.ToArray();
    }

    public override string PacketSignature => "DPSPkt";
}

/*public class DPSPacket : PacketBase {
    [JsonProperty]
    public uint mapNetID;

    [JsonProperty]
    public long dungeonStartTime;

    [JsonProperty]
    public long dungeonClearTime;

    [JsonProperty]
    public long bossTeleportTime;

    [JsonProperty]
    public long bossFightStartTime;

    [JsonProperty]
    public long bossFightEndTime;

    [JsonProperty]
    public List<PacketPlayer> players;

    [JsonProperty]
    public List<DPSValues> bossDamageValues;

    [JsonProperty]
    public List<DPSValues> partyDamageValues;

    public override string PacketSourceGUID => PluginInfo.GUID;

    public DPSPacket() { }

    internal DPSPacket(DungeonInstance instance) {
        mapNetID = instance.mapNetID;
        dungeonStartTime = instance.dungeonStartTime;
        dungeonClearTime = instance.dungeonClearTime;
        bossTeleportTime = instance.bossTeleportTime;
        bossFightStartTime = instance.bossFightStartTime;
        bossFightEndTime = instance.bossFightEndTime;
        bossDamageValues = instance.bossDamage;
        partyDamageValues = instance.totalDungeonDamage;
        players = new List<PacketPlayer>();


        var dict = new Dictionary<uint, DPSValues>();
        foreach (var p in bossDamageValues.Concat(partyDamageValues))
            if (!dict.ContainsKey(p.netId))
                dict[p.netId] = p;

        var dedupedPlayerList = dict.Values.ToArray();
        for (int i = 0; i < dedupedPlayerList.Length; i++)
            players.Add(new PacketPlayer(dedupedPlayerList[i]));
    }

    public static implicit operator bool(DPSPacket obj) {
        return obj != null;
    }
}*/

public class BinaryClientHelloPacket : BinaryPacketBase {

    public string version = PluginInfo.VERSION;
    public string nickname = "null";

    public override void Deserialize(byte[] data) {
        int cursor = 0;
        for (cursor = 0; cursor < data.Length; cursor++)
            if (data[cursor] == 0)
                break;

        version = Encoding.ASCII.GetString(data, 0, cursor);
        nickname = Encoding.ASCII.GetString(data, ++cursor, data.Length - cursor - 1);
    }

    public override byte[] Serialize() {
        int packetSize = PluginInfo.VERSION.Length + 1 + nickname.Length + 1;
        byte[] packet = new byte[packetSize];
        Array.Copy(Encoding.ASCII.GetBytes(PluginInfo.VERSION), 0, packet, 0, PluginInfo.VERSION.Length);
        Array.Copy(Encoding.ASCII.GetBytes(nickname), 0, packet, PluginInfo.VERSION.Length + 1, nickname.Length);

        return packet;
    }

    public override string PacketSignature => "DPSCH"; // DPS Client Hello
}

/*public class DPSClientHelloPacket : PacketBase {
    [JsonProperty]
    public string version = PluginInfo.VERSION;

    [JsonProperty]
    public string nickname = "null";

    public override string PacketSourceGUID => PluginInfo.GUID;
}*/

public class BinaryServerHelloPacket : BinaryPacketBase {

    public string version = PluginInfo.VERSION;

    public override void Deserialize(byte[] data) {
        Encoding.ASCII.GetString(data);
    }

    public override byte[] Serialize() {
        return Encoding.ASCII.GetBytes(version);
    }

    public override string PacketSignature => "DPSSH"; // DPS Server Hello
}

/*public class DPSServerHelloPacket : PacketBase {
    [JsonProperty]
    public string response = "Hello";

    [JsonProperty]
    public string version = PluginInfo.VERSION;

    public override string PacketSourceGUID => PluginInfo.GUID;
}*/