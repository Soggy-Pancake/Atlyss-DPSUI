using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public int value;

    public PacketDPSValue() { }

    public PacketDPSValue(int index, int _value) {
        playerIndex = unchecked((byte)index); // Server should be unusable long before this overflows
        value = _value;
    }
}

public class DPSPacket : PacketBase {
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

    }

    public static implicit operator bool(DPSPacket obj) {
        return obj != null;
    }
}

public class DPSClientHelloPacket : PacketBase {
    [JsonProperty]
    public string version = PluginInfo.VERSION;

    [JsonProperty]
    public string nickname = "null";

    public override string PacketSourceGUID => PluginInfo.GUID;
}

public class DPSServerHelloPacket : PacketBase {
    [JsonProperty]
    public string response = "Hello";

    [JsonProperty]
    public string version = PluginInfo.VERSION;

    public override string PacketSourceGUID => PluginInfo.GUID;
}