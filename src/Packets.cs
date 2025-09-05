using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeTalker.Packets;
using Newtonsoft.Json;

namespace Atlyss_DPSUI {

    public struct DamageHistory {
        public float time;
        public int damage;
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
    
}
