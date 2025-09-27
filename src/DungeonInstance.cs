using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeTalker.Networking;
using Mirror;
using UnityEngine;

namespace Atlyss_DPSUI;

public class DPSValues : Boolable {

    public uint netId;
    public uint color;
    public uint totalDamage;
    public string nickname;
    public string classIcon;

    public DPSValues() { }

    public DPSValues(Player player, uint initialDamage) {
        netId = player.netId;
        nickname = player._nickname;
        totalDamage = initialDamage;

        classIcon = "_clsIco_novice";
        ScriptablePlayerBaseClass sPClass = player._pStats._class;

        if (player._pStats.Network_syncClassTier != 0) {
            int classTier = player._pStats.Network_syncClassTier - 1;
            if (sPClass._playerClassTiers.Length > classTier) {
                Sprite icon = sPClass._playerClassTiers[classTier]._classTierIcon;
                if (icon != null) {
                    classIcon = icon.name;
                } else {
                    classIcon = "Null";
                }
            }
        } else
            classIcon = sPClass._classIcon.name;


        Color32 playerColor;
        try {
            playerColor = player._pVisual._blockOrbRender.material.GetColor("_EmissionColor");
        } catch {
            playerColor = sPClass._blockEmissionColor;
        }

        color = (uint)((playerColor.r << 24) | (playerColor.g << 16) | (playerColor.b << 8) | playerColor.a);
    }

    internal DPSValues(PacketPlayer player, uint damage) { 
        netId = player.netId;
        nickname = player.nickname;
        classIcon = player.icon;
        totalDamage = damage;
    }
}


internal class DungeonInstance {

    public MapInstance map;
    public PatternInstanceManager patternManager;
    public CreepSpawner bossSpawner;
    public Creep bossEntity;

    public uint mapNetID;
    public uint bossEntityNetID;

    public long dungeonStartTime;
    public long dungeonClearTime;
    public long bossTeleportTime;
    public long bossFightStartTime;
    public long bossFightEndTime;

    public float lastSentUpdate;

    public List<DPSValues> bossDamage;
    public List<DPSValues> totalDungeonDamage;

    private bool haveLoadedPlayer;

    private bool Dirty;

    public DungeonInstance(MapInstance newMap) {
        map = newMap;
        patternManager = map._patternInstance;
        if (patternManager)
            bossSpawner = patternManager._bossSpawner;

        mapNetID = newMap.netId;
        if (newMap._zoneType != ZoneType.Dungeon)
            haveLoadedPlayer = true;

        bossDamage = new List<DPSValues>();
        totalDungeonDamage = new List<DPSValues>();
    }

    /// <summary>
    /// Marks the instance as dirty, causing it to send an update to clients when the minimum update wait time has passed since the last update.
    /// </summary>
    public void markDirty() => Dirty = true;

    private void sendPacket() {
        //CodeTalkerNetwork.SendNetworkPacket(new DPSPacket(this));
        CodeTalkerNetwork.SendBinaryNetworkPacket(new BinaryDPSPacket(this));
        Plugin.logger.LogDebug($"Sent update packet for dungeon instance {mapNetID}");
        lastSentUpdate = Time.time;
        Dirty = false;
    }

    public void Print() {
        try {
            Plugin.logger.LogInfo($"Dungeon instance ({map._mapName}) [netId: {mapNetID}] Dirty? {Dirty} LastUpdate: {lastSentUpdate}");
            Plugin.logger.LogInfo($"Pattern manager: {patternManager}");

            if (patternManager) {
                Plugin.logger.LogInfo($"PatternInstance state: isBossEngaged {patternManager._isBossEngaged} netowrkversion {patternManager.Network_isBossEngaged}");
                Plugin.logger.LogInfo($"Boss entity: {bossEntity}");
            }

            Plugin.logger.LogInfo("Dungeon cleared? " + ((dungeonClearTime != 0) ? "Yes" : "No"));

            string bossStatus = "Dormant";
            if (bossFightStartTime != 0)
                bossStatus = "In battle";

            if (bossFightEndTime != 0)
                bossStatus = "Defeated";

            Plugin.logger.LogInfo("Boss status: (" + bossStatus + ")");
            if (bossEntity)
                Plugin.logger.LogInfo($"Boss max hp: {bossEntity._statStruct._maxHealth}");

            Plugin.logger.LogInfo($"Dungeon start time: {dungeonStartTime}");
            Plugin.logger.LogInfo($"Dungeon clear time: {dungeonClearTime}");
            Plugin.logger.LogInfo($"Fight start time: {bossFightStartTime}");
            Plugin.logger.LogInfo($"Fight end time: {bossFightEndTime}");
            if (bossStatus == "Defeated")
                Plugin.logger.LogInfo($"Boss defeated in {(bossFightEndTime - bossFightStartTime) / 1000f} seconds");

            Plugin.logger.LogInfo("Current player boss damage totals: ");
            foreach (DPSValues damage in bossDamage)
                Plugin.logger.LogInfo($"{damage.nickname} ({damage.netId}): {damage.totalDamage} damage");

            Plugin.logger.LogInfo("Current player damage totals: ");
            foreach (DPSValues val in totalDungeonDamage)
                Plugin.logger.LogInfo($"{val.nickname} ({val.netId}): {val.totalDamage} damage");

        } catch (Exception e) {
            Plugin.logger.LogError("Error printing dungeon instance info: " + e);
        }
    }

    public void Update() {
        if (!Plugin._SoloMode && Dirty && Time.time >= lastSentUpdate + DPSUI_Config.clientUpdateRate.Value &&
                (dungeonStartTime > 0 || (bossFightEndTime == 0 && bossFightStartTime > 0))
            ) {
            sendPacket();
        }

        if (!haveLoadedPlayer) {
            foreach (Player player in map._peersInInstance) {
                if (player && player.Network_currentGameCondition == GameCondition.IN_GAME) {
                    haveLoadedPlayer = true;
                    dungeonStartTime = DateTime.UtcNow.Ticks / 10000;
                }
            }
        }
        if (patternManager) {
            if (dungeonClearTime == 0 && patternManager.Network_allArenasBeaten) {
                dungeonClearTime = DateTime.UtcNow.Ticks / 10000;
                if (Plugin._SoloMode)
                    Plugin.AddChatMessage($"[DPSUI] Dungeon cleared in {(float)(dungeonClearTime - dungeonStartTime) / 1000f} seconds! (all arenas beaten)");
            }

            if (patternManager._bossRoomTeleporter && bossTeleportTime == 0 && patternManager._bossRoomTeleporter.Network_allPlayersInTeleporter) {
                bossTeleportTime = DateTime.UtcNow.Ticks / 10000;
                if (Plugin._SoloMode)
                    Plugin.AddChatMessage($"[DPSUI] Boss reached in {(float)(bossTeleportTime - dungeonStartTime) / 1000f} seconds!");
            }

            if (!patternManager.Network_isBossDefeated && patternManager.Network_isBossEngaged && bossFightStartTime == 0) {
                bossFightStartTime = DateTime.UtcNow.Ticks / 10000;
                Plugin.logger.LogInfo("boss engaged " + bossFightStartTime);

                if (bossSpawner && bossSpawner._spawnedCreeps.Count > 0) {
                    bossEntity = bossSpawner._spawnedCreeps[0];
                    bossEntityNetID = bossEntity.netId;

                    if (bossEntity.Network_aggroedEntity._isPlayer)
                        RecordDamage(bossEntity.Network_aggroedEntity._isPlayer, 0, true);

                    foreach (Player player in bossSpawner._playersWithinSpawnerRadius)
                        if (player)
                            RecordDamage(player, 0, true);

                } else {
                    Plugin.logger.LogError("Boss is engaged but boss not found!");
                }
            }
            if (patternManager.Network_isBossDefeated && bossFightStartTime > 0 && bossFightEndTime == 0) {
                bossFightEndTime = DateTime.UtcNow.Ticks / 10000;
                Plugin.logger.LogInfo("Dungeon Boss Beaten!");
                Print();
                Plugin.logger.LogInfo(" ");
                if (!Plugin._SoloMode && Plugin._AmServer) {
                    sendPacket();
                    Plugin.logger.LogDebug("Sent final update packet");
                }

                if (Plugin._SoloMode) {
                    Plugin.AddChatMessage($"[DPSUI] Boss beaten in {(float)(bossFightEndTime - bossFightStartTime) / 1000f} seconds!");
                    Plugin.AddChatMessage($"[DPSUI] Dungeon finished in {(float)(bossFightEndTime - dungeonStartTime) / 1000f} seconds!");
                }
            }
            return;
        }
        if (!bossSpawner) {
            CreepSpawner[] array = Resources.FindObjectsOfTypeAll<CreepSpawner>();
            foreach (CreepSpawner creepSpawner in array) {
                Plugin.logger?.LogInfo(creepSpawner.name + " spawn count: " + creepSpawner._creepCount);
                if (creepSpawner._creepToSpawn != null && PluginInfo.FIELD_BOSSES.Contains(creepSpawner._creepToSpawn._creepName)) {
                    bossSpawner = creepSpawner;
                    break;
                }
            }

            if (!bossSpawner)
                return;
        }
        if (bossFightStartTime == 0) {
            if (!bossSpawner || bossSpawner._spawnedCreeps.Count == 0 || bossSpawner._spawnedCreeps[0] == null)
                return;

            if (bossEntity == null) {
                bossEntity = bossSpawner._spawnedCreeps[0];
                bossEntityNetID = bossEntity.netId;
            }

            if (bossEntity._aggroedEntity == null)
                return;

            bossFightStartTime = DateTime.UtcNow.Ticks / 10000;
            Plugin.logger.LogInfo($"field boss engaged {bossFightStartTime}");

            if (bossEntity.Network_aggroedEntity._isPlayer)
                RecordDamage(bossEntity.Network_aggroedEntity._isPlayer, 0, true);

            foreach (Player player in bossSpawner._playersWithinSpawnerRadius)
                RecordDamage(player, 0, true);
        }
        if (bossFightStartTime > 0 && bossFightEndTime == 0 && bossEntity._statusEntity._currentHealth <= 0) {
            bossFightEndTime = DateTime.UtcNow.Ticks / 10000;
            Plugin.logger.LogInfo("Field boss killed!");
            Print();
            Plugin.logger.LogInfo(" ");

            if (!Plugin._SoloMode && Plugin._AmServer) {
                sendPacket();
                Plugin.logger.LogDebug("Sent final update packet");
            }

            if (Plugin._SoloMode) {
                Plugin.AddChatMessage($"[DPSUI] Boss beaten in {(float)(bossFightEndTime - bossFightStartTime) / 1000f} seconds!");
            }

            bossFightStartTime = 0;
            bossFightEndTime = 0;
            bossDamage.Clear();
        }
    }

    public void RecordDamage(Player player, int damage, bool isBossDamage) {
        if (!player)
            return;

        if (isBossDamage) {
            bool foundPlayer = false;
            for (int i = 0; i < bossDamage.Count; i++) {
                if (bossDamage[i].netId == player.netId) {
                    foundPlayer = true;
                    bossDamage[i].totalDamage += unchecked((uint)(bossDamage[i].totalDamage + damage));
                }
            }

            if (!foundPlayer)
                bossDamage.Add(new DPSValues(player, (uint)damage));
        } else {
            if (player.Network_playerMapInstance._zoneType != ZoneType.Dungeon)
                return;

            bool foundPlayer = false;
            for (int i = 0; i < totalDungeonDamage.Count; i++) {
                if (totalDungeonDamage[i].netId == player.netId) {
                    foundPlayer = true;
                    totalDungeonDamage[i].totalDamage += unchecked((uint)(bossDamage[i].totalDamage + damage));
                }
            }

            if (!foundPlayer)
                totalDungeonDamage.Add(new DPSValues(player, (uint)damage));
        }

        Dirty = true;
    }
}