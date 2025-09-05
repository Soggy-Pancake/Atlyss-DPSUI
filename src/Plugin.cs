using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CodeTalker.Packets;
using CodeTalker.Networking;

//using Object = UnityEngine.Object;


namespace Atlyss_DPSUI {

    [BepInDependency("CodeTalker", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("EasySettings", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("soggy_pancake.plugins.atlyssDPSUI", "AtlyssDPSUI", PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin {

        public static Plugin AtlyssDPSUI;
        internal static Harmony _harmony;
        internal static ManualLogSource logger;

        internal static Player player;
        internal static InGameUI GameUI;
        public static Text localDpsText;

        internal static bool _serverSupport;
        internal static bool _AmServer;
        internal static bool _AmHeadless;

        internal static bool _SoloMode;

        internal static bool shownDungeonClearText;

        internal static List<DungeonInstance> dungeonInstances = new List<DungeonInstance>();
        internal static List<DamageHistory> localDamage = new List<DamageHistory>();

        internal static DPSPacket lastDPSPacket;

        internal static void Client_ParseDungeonPartyDamage(PacketHeader header, PacketBase packet) {
            if (!(packet is DPSPacket dPSPacket) || !Player._mainPlayer.NC()?.Network_playerMapInstance || dPSPacket.mapNetID != Player._mainPlayer.Network_playerMapInstance.netId) {
                return;
            }

            logger.LogDebug("Recieved valid packet for instance!");
            logger.LogDebug($"Boss fight start {dPSPacket.bossFightStartTime}!");
            logger.LogDebug($"Boss fight end   {dPSPacket.bossFightEndTime}!");

            if (!lastDPSPacket) {
                lastDPSPacket = dPSPacket;
            }

            if (lastDPSPacket.dungeonClearTime == 0 && dPSPacket.dungeonClearTime > 0) {
                AddChatMessage($"[DPSUI] Dungeon cleared in {(float)(dPSPacket.dungeonClearTime - dPSPacket.dungeonStartTime) / 1000f} seconds!(arenas only)");
            }

            if (lastDPSPacket.bossTeleportTime == 0 && dPSPacket.bossTeleportTime > 0) {
                AddChatMessage($"[DPSUI] Boss reached in {(float)(dPSPacket.bossTeleportTime - dPSPacket.dungeonStartTime) / 1000f} seconds!");
            }

            if (lastDPSPacket.bossFightEndTime == 0 && dPSPacket.bossFightEndTime > 0 && dPSPacket.dungeonStartTime > 0) {
                AddChatMessage($"[DPSUI] Boss beaten in {(float)(dPSPacket.bossFightEndTime - dPSPacket.bossFightStartTime) / 1000f} seconds!");
                AddChatMessage($"[DPSUI] Dungeon finished in {(float)(dPSPacket.bossFightEndTime - dPSPacket.dungeonStartTime) / 1000f} seconds!");
            }

            int totalDamage = 0;
            foreach (DPSValues bossDamageValue in dPSPacket.bossDamageValues) {
                totalDamage += bossDamageValue.totalDamage;
            }

            DPSUI_GUI._UI.UpdatePartyDamageValues(dPSPacket);
            lastDPSPacket = dPSPacket;
        }

        internal static void AddChatMessage(string msg) {
            Player._mainPlayer.NC()?._chatBehaviour.NC()?.New_ChatMessage(msg);
        }

        internal static void AddGameFeedMessage(string msg) {
            Player._mainPlayer.NC()?._chatBehaviour.NC()?.Init_GameLogicMessage(msg);
        }

        private void Awake() {
            AtlyssDPSUI = this;
            logger = Logger;
            _harmony = new Harmony(PluginInfo.GUID);
            DPSUI_Config.init(Config);

            localDamage = new List<DamageHistory>();
            logger.LogInfo("Patch successful! Registering network listeners...");

            CodeTalkerNetwork.RegisterListener<DPSClientHelloPacket>(ServerPatches.Server_RecieveHello);

            if (!Environment.GetCommandLineArgs().Contains("-server")) {
                _harmony.PatchAll(typeof(ClientPatches));
                CodeTalkerNetwork.RegisterListener<DPSServerHelloPacket>(ClientPatches.Client_RecieveHello);
                CodeTalkerNetwork.RegisterListener<DPSPacket>(Client_ParseDungeonPartyDamage);
            } else {
                logger.LogWarning("Headless mode detected!");
                _serverSupport = _AmHeadless = true;
            }

            _harmony.PatchAll(typeof(ServerPatches));
        }

        private void Update() {
            if (_AmHeadless || !player)
                return;
            
            if (!DPSUI_GUI.createdUI && InGameUI._current) {
                DPSUI_GUI._UI = new DPSUI_GUI();
                _SoloMode = AtlyssNetworkManager._current._soloMode;
            }

            if (GameUI == null)
                GameUI = InGameUI._current;

            DPSUI_GUI._UI.Update();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.RightShift)) {
                logger.LogInfo("main player: " + Player._mainPlayer);
                logger.LogInfo($"Slide in time: {DPSUI_Config.transitionTime.Value}");
                logger.LogInfo($"UI Mode: {DPSUI_GUI._UIMode}");
                ClientPatches.ClientSendHello();
                LogFullHierarchy();

                Portal[] array = FindObjectsOfType<Portal>();
                foreach (Portal portal in array) {
                    try {
                        if (portal.gameObject.activeSelf) {
                            logger.LogInfo(portal.gameObject.name);
                            logger.LogInfo(portal._scenePortal._portalType);
                        }
                    } catch {
                    }
                }

                ScriptablePlayerBaseClass scriptablePlayerBaseClass = player.NC()?._pStats.NC()?._class;
                logger.LogInfo("Player base class name " + scriptablePlayerBaseClass._className);
                if (player._pStats._syncClassTier != 0) {
                    logger.LogInfo("Has subclass!");
                    try {
                        logger.LogInfo("Player base class name " + scriptablePlayerBaseClass._playerClassTiers[Player._mainPlayer._pStats._syncClassTier - 1]._classTierIcon.name);
                    } catch { }
                }
                logger.LogError($"Dungeon instances tracked: {dungeonInstances.Count}");
                foreach (DungeonInstance dungeonInstance in dungeonInstances) {
                    dungeonInstance.Print();
                }
            }

            if (Input.GetKeyDown(DPSUI_Config.togglePartyUIBind.Value)) {
                DPSUI_Config.showPartyUI.Value = (DPSUI_GUI.userShowPartyUI = !DPSUI_GUI.userShowPartyUI);
                AddGameFeedMessage((DPSUI_GUI.userShowPartyUI ? "Enabled" : "Disabled") + " party UI");
            }

            if (Input.GetKeyDown(DPSUI_Config.toggleLocalUIBind.Value)) {
                DPSUI_Config.showLocalUI.Value = (DPSUI_GUI.userShowLocalUI = !DPSUI_GUI.userShowLocalUI);
                AddGameFeedMessage((DPSUI_GUI.userShowLocalUI ? "Enabled" : "Disabled") + " local UI");
            }

            if (Input.GetKeyDown(DPSUI_Config.switchPartyUITypeBind.Value)) {

                if (DPSUI_GUI._UIMode == UIMode.Auto)
                    DPSUI_GUI._UIMode = UIMode.Party;
                else if (DPSUI_GUI._UIMode == UIMode.Party)
                    DPSUI_GUI._UIMode = UIMode.Boss;
                else if (DPSUI_GUI._UIMode == UIMode.Boss)
                    DPSUI_GUI._UIMode = UIMode.Auto;
               
                AddGameFeedMessage($"Set UI Mode to {DPSUI_GUI._UIMode}");
                DPSUI_GUI._UI?.UpdatePartyDamageValues(lastDPSPacket);
            }
        }

        private void FixedUpdate() {
            if (player != Player._mainPlayer) {
                player = Player._mainPlayer;
            }

            if (_AmServer) {
                for (int i = dungeonInstances.Count - 1; i >= 0; i--) {
                    DungeonInstance dungeonInstance = dungeonInstances[i];

                    if (!dungeonInstances[i].map) {
                        dungeonInstances.RemoveAt(i);
                        logger.LogInfo("Dungeon was unloaded!");
                    } else {
                        dungeonInstance.Update();
                    }
                }
            } else if (!_serverSupport && player.NC()?.Network_currentGameCondition == GameCondition.IN_GAME && ClientPatches._helloRetryCount < 5) {
                ClientPatches.ClientSendHello();
            }

            if (_AmHeadless || !DPSUI_GUI.showPartyUI)
                return;

            if (!player) {
                DPSUI_GUI.showPartyUI = false;
                return;
            }

            MapInstance mapInstance = player.Network_playerMapInstance.NC();
            if (mapInstance && mapInstance._zoneType == ZoneType.Field) {
                DPSPacket dPSPacket = lastDPSPacket;
                if (dPSPacket != null && dPSPacket.bossFightEndTime > 0 && lastDPSPacket.bossFightEndTime < DateTime.UtcNow.Ticks / 10000 - 30000) {
                    DPSUI_GUI.showPartyUI = false;
                    logger.LogInfo("Stopped showing field boss info after 30 seconds!");
                }
            }
        }

        private static void LogFullHierarchy() {
            Player mainPlayer = Player._mainPlayer;
            if (SceneManager.sceneCount >= 2) {
                ClientPatches.ClientSendHello(force: true);
                mainPlayer.NC()?._chatBehaviour.NC()?.New_ChatMessage("<color=#fce75d>Server AtlyssDPSUI Version mismatch! (Server version: LogFullHierarchyTest)</color>");
                Debug.Log("Am host: " + (mainPlayer.NC()?.Network_isHostPlayer != null ? "yes" : "nah"));
                Debug.Log("_AmHeadless " + _AmHeadless);
                Debug.Log("Server support " + _serverSupport);
                Debug.Log("AmServer " + _serverSupport);
                Debug.Log("last dps packet " + lastDPSPacket);
                Debug.Log("\nSpawners:");
                CreepSpawner[] array = Resources.FindObjectsOfTypeAll<CreepSpawner>();
                foreach (CreepSpawner creepSpawner in array) {
                    Debug.Log(creepSpawner.name + " spawn count: " + creepSpawner._creepCount);
                }
            }

            Debug.Log(mainPlayer.NC()?._playerMapInstance.NC()?.name);
            Debug.Log(mainPlayer.NC()?._playerMapInstance.NC()?._zoneType);
            Debug.Log($"Loaded scenes: {SceneManager.sceneCount}");

            for (int j = 0; j < SceneManager.sceneCount; j++) {
                Scene scene = SceneManager.GetSceneAt(j);
                Debug.Log("Loaded scene: " + scene.name);
            }
        }

        public static void LogHierarchy(Transform root, int depth = 0) {
            Debug.Log(new string(' ', depth * 2) + root.name);
            if (!root.name.StartsWith("_player(") && !root.name.StartsWith("_raceModelDisplayDolly")) {
                foreach (Transform item in root) {
                    LogHierarchy(item, depth + 1);
                }
                return;
            }
            Debug.Log(new string(' ', (depth + 1) * 2) + "...");
        }
    }
}
