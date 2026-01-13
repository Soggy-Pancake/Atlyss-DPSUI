using System;
using CodeTalker.Networking;
using CodeTalker.Packets;
using HarmonyLib;

namespace Atlyss_DPSUI;

internal class ServerPatches {

    internal static void Server_RecieveHello(PacketHeader header, BinaryPacketBase packet) {
        if (packet is BinaryClientHelloPacket dPSClientHelloPacket && Player._mainPlayer.NC()?.Network_isHostPlayer == true && !header.SenderIsLobbyOwner) {
            Plugin.logger.LogDebug($"Server replying to client! Client name: {dPSClientHelloPacket.nickname}");

            CodeTalkerNetwork.SendNetworkPacket(new BinaryServerHelloPacket());
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
            } catch (Exception e) { 
                Plugin.logger.LogError("Exception while recording damage: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(MapInstance), "Apply_InstanceData")]
    [HarmonyPostfix]
    internal static void OnMapLoad(MapInstance __instance) {
        if (__instance._zoneType != ZoneType.Safe) {
            DungeonInstance dungeonInstance = new DungeonInstance(__instance);
            Plugin.dungeonInstances.Add(dungeonInstance);
        }
    }
}