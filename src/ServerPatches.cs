using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeTalker.Networking;
using CodeTalker.Packets;
using HarmonyLib;
using UnityEngine;

namespace Atlyss_DPSUI;

internal class ServerPatches {
    internal static void Server_RecieveHello(PacketHeader header, PacketBase packet) {
        if (packet is DPSClientHelloPacket dPSClientHelloPacket && Player._mainPlayer.NC()?.Network_isHostPlayer == true && !header.SenderIsLobbyOwner) {
            Plugin.logger.LogInfo("Server replying to client! (" + dPSClientHelloPacket.nickname + ")");

            CodeTalkerNetwork.SendNetworkPacket(new DPSServerHelloPacket());
        }

    internal static void Server_RecieveHello(PacketHeader header, BinaryPacketBase packet) {
        if (packet is BinaryClientHelloPacket dPSClientHelloPacket) { // && Player._mainPlayer.NC()?.Network_isHostPlayer == true && !header.SenderIsLobbyOwner) {
            Plugin.logger.LogInfo("Server replying to client! BINARY (" + dPSClientHelloPacket.nickname + ")");

            CodeTalkerNetwork.SendBinaryNetworkPacket(new BinaryServerHelloPacket());
        }
    }


    [HarmonyPatch(typeof(StatusEntity), "Take_Damage")]
    [HarmonyPostfix]
    internal static void TrackDamage(StatusEntity __instance, DamageStruct _dmgStruct) {
        if (!__instance.NC()?._isCreep)
            return;

        foreach (DungeonInstance dungeonInstance in Plugin.dungeonInstances) {
            try {
                if (dungeonInstance.map == _dmgStruct._statusEntity.NC()?._isPlayer.NC()?.Network_playerMapInstance) {
                    dungeonInstance.RecordDamage(_dmgStruct._statusEntity?._isPlayer, _dmgStruct._damageValue, __instance._isCreep == dungeonInstance.bossEntity);
                    break;
                }
            } catch { }
        }
    }

    [HarmonyPatch(typeof(MapInstance), "Apply_InstanceData")]
    [HarmonyPostfix]
    internal static void OnMapLoad(MapInstance __instance) {
        if (__instance._zoneType == ZoneType.Dungeon || PluginInfo.FIELDS_WITH_BOSSES.Contains(__instance._mapName)) {
            DungeonInstance dungeonInstance = new DungeonInstance(__instance);
            dungeonInstance.Update();
            Plugin.dungeonInstances.Add(dungeonInstance);
        } else {
            Plugin.logger.LogInfo($"{__instance._mapName} not added to dungeon list.");
        }
    }
}