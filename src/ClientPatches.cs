using System;
using System.Collections.Generic;
using System.Linq;
using Atlyss_DPSUI;
using UnityEngine;
using CodeTalker.Packets;
using CodeTalker.Networking;
using HarmonyLib;

namespace Atlyss_DPSUI {
    internal class ClientPatches {

        internal static int _helloRetryCount;
        internal static float _helloRetryLast;

        internal static void ClientSendHello(bool force = false) {
            if (force || (!Player._mainPlayer._isHostPlayer && !(_helloRetryLast > Time.time - 6f))) {
                _helloRetryCount++;
                Plugin.logger.LogDebug($"Sending hello packet (Attempt {_helloRetryCount})");

                DPSClientHelloPacket packet = new DPSClientHelloPacket {
                    nickname = Player._mainPlayer._nickname
                };
                _helloRetryLast = Time.time;

                CodeTalkerNetwork.SendNetworkPacket(packet);
            }
        }

        internal static void Client_RecieveHello(PacketHeader header, PacketBase packet) {
            if (Plugin.player.NC()?.Network_isHostPlayer == true || !(packet is DPSServerHelloPacket dPSServerHelloPacket))
                return;
            
            Plugin.logger.LogInfo("Client recieved hello. Server DPSUI version is " + dPSServerHelloPacket.version);
            if (!header.SenderIsLobbyOwner)
                return;

            if (dPSServerHelloPacket.response == "Hello") {
                if (dPSServerHelloPacket.version != PluginInfo.VERSION) {
                    Plugin.logger.LogWarning("Server version mismatch!");
                    Player._mainPlayer._chatBehaviour.New_ChatMessage("<color=#fce75d>Server AtlyssDPSUI Version mismatch! (Server version: " + dPSServerHelloPacket.version + ")</color>");
                }
                Plugin._serverSupport = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtlyssNetworkManager), "OnStartClient")]
        [HarmonyPatch(typeof(AtlyssNetworkManager), "OnStopClient")]
        internal static void ServerChange() {
            _helloRetryCount = 0;
            Plugin._serverSupport = false;
            DPSUI_GUI.createdUI = false;
            DPSUI_GUI._UI = null;
            Plugin.player = null;
            DPSUI_GUI.showPartyUI = false;
            Plugin.logger.LogInfo("Server status change, server support false");
        }

        [HarmonyPatch(typeof(Player), "OnPlayerMapInstanceChange")]
        [HarmonyPostfix]
        internal static void OnMapChanged(MapInstance _old, MapInstance _new) {
            if (_new != _old) {
                if (!Plugin._serverSupport && _helloRetryCount < 5)
                    ClientSendHello();

                Plugin.player = Player._mainPlayer;
                Plugin._AmServer = Player._mainPlayer.Network_isHostPlayer;
                DPSUI_GUI.showPartyUI = false;

                Plugin.shownDungeonClearText = false;
                Plugin.localDamage?.Clear();
                if (DPSUI_GUI.createdUI) {
                    DPSUI_GUI._UI?.UpdateDPS(0);
                    DPSUI_GUI._UI?.clearDamageValues();
                    DPSUI_GUI.resetPartyAnim();
                }
            }
        }

        [HarmonyPatch(typeof(StatusEntityGUI), "UserCode_Target_Display_FloatingNumber__StatusEntity__FloatTextColor__Int32__Int32__Int32")]
        [HarmonyPostfix]
        private static void TrackOwnDamage(StatusEntity _followObj, FloatTextColor _floatTextColor, int _value, int _combatElementID, int _conditionGroupID) {
            if (!Player._mainPlayer || _followObj == Player._mainPlayer.NC()?._statusEntity || _floatTextColor == FloatTextColor.MISS || _floatTextColor == FloatTextColor.EXPERIENCE || _floatTextColor == FloatTextColor.HEAL) {
                return;
            }

            float currentTime = Time.unscaledTime;
            DamageHistory item = new DamageHistory {
                time = currentTime,
                damage = _value
            };

            List<DamageHistory> localDamage = Plugin.localDamage;

            if (DPSUI_Config.keepDamageUntilPause.Value) {
                if (localDamage.Count > 0 && currentTime - localDamage[localDamage.Count - 1].time >= DPSUI_Config.damageHoldTime.Value) {
                    localDamage.Clear();
                }
                localDamage.Add(item);
            } else {
                localDamage.RemoveAll((DamageHistory hit) => currentTime - hit.time >= DPSUI_Config.damageHoldTime.Value);
                localDamage.Add(item);
            }

            float lastHitTime = localDamage[0].time;
            int totalDamage = 0;
            foreach (DamageHistory hit in localDamage)
                totalDamage += hit.damage;

            if (currentTime == lastHitTime)
                lastHitTime -= 1f;

            float dps = totalDamage / Mathf.Max(0.5f, currentTime - lastHitTime);
            Plugin.logger.LogDebug($"Hits tracked: {localDamage.Count} Current hit dmg value: {_value} total time between last valid hit and current: {currentTime - lastHitTime}");
            DPSUI_GUI._UI.UpdateDPS(dps);
        }

        [HarmonyPatch(typeof(PlayerInteract), "Cmd_InteractWithPortal")]
        [HarmonyPrefix]
        private static void ClientPortalInteraction(Portal _portal, ZoneDifficulty _setDifficulty) {
            Plugin.logger.LogDebug("Portal interaction! portal tag: " + _portal.NC()._scenePortal?._spawnPointTag);
            if (_portal.NC()?._scenePortal == null)
                return;

            Player player = Player._mainPlayer.NC();
            if (player != null && player.Network_playerZoneType == ZoneType.Dungeon && _portal._scenePortal._spawnPointTag == "fortSpawn") {
                DPSPacket lastDPSPacket = Plugin.lastDPSPacket;
                if (lastDPSPacket != null && lastDPSPacket.bossFightEndTime == 0)
                    Plugin.logger.LogDebug("Portaled out of finished dungeon!");
                Plugin.logger.LogDebug("Portaled out of dungeon early?");
            }
        }
    }
}