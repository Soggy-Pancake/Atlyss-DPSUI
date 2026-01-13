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
using BepInEx.Bootstrap;
using Mirror;

namespace Atlyss_DPSUI;
#pragma warning disable CS8618 

[BepInDependency("CodeTalker", "1.3.0")]
[BepInDependency("EasySettings", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("Soggy_Pancake.atlyssDPSUI", "AtlyssDPSUI", PluginInfo.VERSION)]
public class Plugin : BaseUnityPlugin {

    public static Plugin AtlyssDPSUI;
    internal static Harmony _harmony;
    internal static ManualLogSource logger;

    internal static Player? player;
    internal static InGameUI GameUI;
    public static Text localDpsText;

    internal static bool _serverSupport;
    internal static bool _AmServer;
    internal static bool _AmHeadless;

    internal static bool _SoloMode;

    internal static bool shownDungeonClearText;

    internal static List<DungeonInstance> dungeonInstances = new List<DungeonInstance>();
    internal static List<DamageHistory> localDamage = new List<DamageHistory>();

    internal static BinaryDPSPacket lastDPSPacket;

    internal static void Client_ParseDungeonPartyDamage(PacketHeader header, BinaryPacketBase packet) {
        if (!(packet is BinaryDPSPacket dPSPacket) || !Player._mainPlayer.NC()?.Network_playerMapInstance ||
            dPSPacket.mapNetID != Player._mainPlayer.Network_playerMapInstance.netId) {
            return;
        }

        /*logger.LogDebug("Recieved valid BINARY packet for instance!");
        logger.LogDebug($"Map net ID     {dPSPacket.mapNetID}!");
        logger.LogDebug($"Dungeon start   {dPSPacket.dungeonStartTime}!");
        logger.LogDebug($"Dungeon clear   {dPSPacket.dungeonClearTime}!");
        logger.LogDebug($"Boss teleport   {dPSPacket.bossTeleportTime}!");
        logger.LogDebug($"Boss fight start {dPSPacket.bossFightStartTime}!");
        logger.LogDebug($"Boss fight end   {dPSPacket.bossFightEndTime}!");

        logger.LogDebug($"Players in packet: {dPSPacket.players.Count}");
        logger.LogDebug($"Boss damage list len {dPSPacket.bossDamageValues.Count}");
        logger.LogDebug($"Party damage list len {dPSPacket.partyDamageValues.Count}");

        logger.LogDebug("Players:");
        foreach(var p in dPSPacket.players) {
            logger.LogDebug($"{p.netId}: {p.nickname} {p.icon} {p.color}");
        }

        logger.LogDebug("\nBoss damage values:");
        foreach (var p in dPSPacket.bossDamageValues) {
            logger.LogDebug(p);
        }

        logger.LogDebug("\nParty damage values:");
        foreach (var p in dPSPacket.partyDamageValues) {
            logger.LogDebug(p);
        }*/

        if (lastDPSPacket == null)
            lastDPSPacket = dPSPacket;
        

        if (DPSUI_Config.speedyBoiMode.Value && lastDPSPacket.dungeonClearTime == 0 && dPSPacket.dungeonClearTime > 0) {
            AddChatMessage($"[DPSUI] Dungeon cleared in {(float)(dPSPacket.dungeonClearTime - dPSPacket.dungeonStartTime) / 1000f} seconds!(arenas only)");
        }

        if (DPSUI_Config.speedyBoiMode.Value && lastDPSPacket.bossTeleportTime == 0 && dPSPacket.bossTeleportTime > 0) {
            AddChatMessage($"[DPSUI] Boss reached in {(float)(dPSPacket.bossTeleportTime - dPSPacket.dungeonStartTime) / 1000f} seconds!");
        }

        if (DPSUI_Config.speedyBoiMode.Value && lastDPSPacket.bossFightEndTime == 0 && dPSPacket.bossFightEndTime > 0 && dPSPacket.dungeonStartTime > 0) {
            AddChatMessage($"[DPSUI] Boss beaten in {(float)(dPSPacket.bossFightEndTime - dPSPacket.bossFightStartTime) / 1000f} seconds!");
            AddChatMessage($"[DPSUI] Dungeon finished in {(float)(dPSPacket.bossFightEndTime - dPSPacket.dungeonStartTime) / 1000f} seconds!");
        }

        DPSUI_GUI._UI?.UpdatePartyDamageValues(dPSPacket);
        lastDPSPacket = dPSPacket;
    }

    internal static void AddChatMessage(string msg) => Player._mainPlayer.NC()?._chatBehaviour.NC()?.New_ChatMessage(msg);

    internal static void AddGameFeedMessage(string msg) => Player._mainPlayer.NC()?._chatBehaviour.NC()?.Init_GameLogicMessage(msg);

    private void Awake() {
        AtlyssDPSUI = this;
        logger = Logger;
        _harmony = new Harmony(PluginInfo.GUID);
        DPSUI_Config.init(Config);

        if(Application.version != PluginInfo.GAME_VERSION) {
            logger.LogWarning($"[VERSION MISMATCH] This version of AtlyssDPSUI is made for game version {PluginInfo.GAME_VERSION}, you are running {Application.version}. Unexpected issues may occur.");
        }

        localDamage = new List<DamageHistory>();
        _harmony.PatchAll(typeof(ServerPatches));
        logger.LogInfo("Patch successful! Registering network listeners...");

        CodeTalkerNetwork.RegisterBinaryListener<BinaryClientHelloPacket>(ServerPatches.Server_RecieveHello);

        if (!Environment.GetCommandLineArgs().Contains("-server")) {
            _harmony.PatchAll(typeof(ClientPatches));
            CodeTalkerNetwork.RegisterBinaryListener<BinaryServerHelloPacket>(ClientPatches.Client_RecieveHello);
            CodeTalkerNetwork.RegisterBinaryListener<BinaryDPSPacket>(Client_ParseDungeonPartyDamage);
        } else {
            logger.LogWarning("Headless mode detected!");
            _serverSupport = _AmHeadless = true;
        }

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

        DPSUI_GUI._UI?.Update();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.RightShift)) {
            logger.LogInfo("main player: " + Player._mainPlayer);
            logger.LogInfo($"Slide in time: {DPSUI_Config.transitionTime.Value}");
            logger.LogInfo($"UI Mode: {DPSUI_GUI._UIMode}");
            logger.LogInfo($"Update rate: {DPSUI_Config.clientUpdateRate.Value}");

            /*Portal[] array = FindObjectsOfType<Portal>();
            foreach (Portal portal in array) {
                try {
                    if (portal.gameObject.activeSelf) {
                        logger.LogInfo(portal.gameObject.name);
                        logger.LogInfo(portal._scenePortal._portalType);
                    }
                } catch {
                }
            }*/

            if (NetworkClient.isConnected) {
                logger.LogInfo(Transport.active.ServerUri().ToString());
                logger.LogInfo(SteamLobby._current._currentLobbyID);
                logger.LogInfo(AtlyssNetworkManager._current.networkAddress);
            }

            ScriptablePlayerBaseClass? scriptablePlayerBaseClass = player?.NC()?._pStats.NC()?._class;
            logger.LogInfo("Player base class name " + scriptablePlayerBaseClass?.NC()?._className);
            if (player?._pStats?._syncClassTier != 0) {
                logger.LogInfo("Has subclass!");
                try {
                    logger.LogInfo("Player base class name " + scriptablePlayerBaseClass?._playerClassTiers[Player._mainPlayer._pStats._syncClassTier - 1]._classTierIcon.name);
                } catch { }
            }
            logger.LogError($"Dungeon instances tracked: {dungeonInstances.Count}");
            foreach (DungeonInstance dungeonInstance in dungeonInstances) {
                dungeonInstance.Print();
            }
        }

        if (player?.NC() != null && !player._inChat && !player._inUI) {
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
                    logger.LogDebug($"Dungeon was unloaded!");
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

        MapInstance? mapInstance = player?.NC()?.Network_playerMapInstance?.NC();
        if (mapInstance != null && mapInstance._zoneType == ZoneType.Field) {
            BinaryDPSPacket dPSPacket = lastDPSPacket;
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
            //mainPlayer.NC()?._chatBehaviour.NC()?.New_ChatMessage("<color=#fce75d>Server AtlyssDPSUI Version mismatch! (Server version: LogFullHierarchyTest)</color>");
            logger.LogInfo("Am host: " + (mainPlayer.NC()?.Network_isHostPlayer != null ? "yes" : "nah"));
            logger.LogInfo("_AmHeadless " + _AmHeadless);
            logger.LogInfo("Server support " + _serverSupport);
            logger.LogInfo("AmServer " + _serverSupport);
            logger.LogInfo("last dps packet " + lastDPSPacket);
            logger.LogInfo($"Show partyUI: {DPSUI_GUI.showPartyUI}; Show localUI: {DPSUI_GUI.showLocalUI}; Player in ui: {player?._inUI}");
            if (_AmServer) {
                logger.LogInfo("\nSpawners:");
                CreepSpawner[] array = Resources.FindObjectsOfTypeAll<CreepSpawner>();
                foreach (CreepSpawner creepSpawner in array) {
                    logger.LogInfo(creepSpawner.name + " spawn count: " + creepSpawner._creepCount);
                }
            }
        }

        logger.LogInfo(mainPlayer.NC()?._playerMapInstance.NC()?.name);
        logger.LogInfo(mainPlayer.NC()?._playerMapInstance.NC()?._zoneType);
        logger.LogInfo($"Loaded scenes: {SceneManager.sceneCount}");

        for (int j = 0; j < SceneManager.sceneCount; j++) {
            Scene scene = SceneManager.GetSceneAt(j);
            logger.LogInfo("Loaded scene: " + scene.name);
        }
    }
}