using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserClearing
{
    public class LocalLaser_Patch
    {
        public static bool Enable = true;
        public static bool EnableLoot = true;
        public static int RequiredSpace = 5;
        public static int MaxLaserCount = 3;
        public static float Range = 40f;
        public static int MiningTick = 60;        
        public static double MiningPower = 6000; // 1000 per tick = 60kw in game
        public static bool DropOnly = true;
        public static bool SpaceCapsule = false;
        public static bool EnableDestructionSFX = false;
        public static float ScaleWithDroneCount = 0f;
        public static float ScaleWithMiningSpeed = 0f;

        static readonly Dictionary<int, bool> checkVeges = new(); // true: avaible target
        static readonly List<int> laserIds = new();
        static int factoryIndex = -1;
        static int maxLaserCount = 3;
        static int tickToClear = 60;
        static int checkIntervalTick = 10;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.NotifyTechUnlock))]
        static void SetParameters()
        {
            maxLaserCount = MaxLaserCount;
            if (ScaleWithDroneCount > 0f)
            {
                maxLaserCount = (int)(maxLaserCount * (GameMain.mainPlayer.mecha.constructionModule.droneCount) * ScaleWithDroneCount);
            }
            maxLaserCount = maxLaserCount > 0 ? maxLaserCount : 1;
            tickToClear = MiningTick;
            if (ScaleWithMiningSpeed > 0f)
            {
                tickToClear = (int)(tickToClear / (1 + (GameMain.history.miningSpeedScale - 1f) * ScaleWithMiningSpeed));
            }
            tickToClear = tickToClear > 0 ? tickToClear : 1;
            checkIntervalTick = tickToClear / (maxLaserCount * 2);
            checkIntervalTick  = checkIntervalTick > 0 ? checkIntervalTick : 1;
            Plugin.Log.LogDebug($"maxLaserCount={maxLaserCount} tickToClear={tickToClear}");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerAction_Mine), nameof(PlayerAction_Mine.GameTick))]
        static void GameTick(PlayerAction_Mine __instance)
        {
            if (!Enable) return;
            PlanetFactory factory = __instance.player.factory;
            if (factory == null) return; // Don't run in space
            if (factoryIndex != factory.index) ClearAll();
            factoryIndex = factory.index;
            Mecha mecha = __instance.player.mecha;
            Vector3 beginPos = mecha.skillCastRightL;

            if (laserIds.Count < maxLaserCount && GameMain.gameTick % checkIntervalTick == 0)
            {
                if (CheckPlayerInventorySpace())
                {
                    UI_Patch.UpdateButtonStatus(UI_Patch.ButtonStatus.Normal);
                    int vegeId = GetClosestVegeId(factory, beginPos, Range);
                    if (vegeId != 0)
                    {
                        laserIds.Add(StartLaser(factory, vegeId, beginPos));
                        checkVeges[vegeId] = false; // Now targetting
                    }
                }
                else
                {
                    UI_Patch.UpdateButtonStatus(UI_Patch.ButtonStatus.NotEnoughSpace);
                }
            }
            for (int i = laserIds.Count - 1; i >= 0; i--)
            {
                if (ContinueLaser(factory, laserIds[i], beginPos, out int vegeId))
                {
                    ref var vege = ref factory.vegePool[vegeId];
                    mecha.QueryEnergy(MiningPower, out double energyConsumed, out _);
                    mecha.coreEnergy -= energyConsumed;
                    mecha.MarkEnergyChange(5, -energyConsumed);
                }
                else
                {
                    checkVeges.Remove(vegeId);
                    laserIds.RemoveAt(i);
                }
            }
        }

        private static bool CheckPlayerInventorySpace()
        {
            if (!EnableLoot) return true;
            var grids = GameMain.mainPlayer.package.grids;
            var spaceCount = 0;
            for (int i = GameMain.mainPlayer.package.size - 1; i >= 0; i--)
            {
                if (grids[i].count == 0 && grids[i].filter == 0) ++spaceCount;
                if (spaceCount >= RequiredSpace) return true;
            }
            return false;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void ClearAll()
        {
            checkVeges.Clear();
            laserIds.Clear();
            StopAll();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.KillVegeFinally))]
        public static bool KillVegeFinally_Prefix(PlanetFactory __instance, int id)
        {
            if (!Enable || __instance.index != factoryIndex)
                return true;

            ref var vegeData = ref __instance.vegePool[id];
            VegeProto vegeProto = LDB.veges.Select(vegeData.protoId);
            if (vegeProto != null)
            {
                VFEffectEmitter.Emit(vegeProto.MiningEffect, __instance.vegePool[id].pos, __instance.vegePool[id].rot);
                if (EnableDestructionSFX)
                    VFAudio.Create(vegeProto.MiningAudio, null, __instance.vegePool[id].pos, true, 1, -1, -1L);

                if (EnableLoot)
                {
                    GetVegLoot(__instance, vegeData, vegeProto);
                }
            }
            __instance.RemoveVegeWithComponents(id);
            return false;
        }

        static void GetVegLoot(PlanetFactory factory, in VegeData vegeData, VegeProto vegeProto)
        {
            // From PlayerAction_Mine.GameTick()
            var dotNet35Random = new DotNet35Random(vegeData.id + ((factory.planet.seed & 16383) << 14));
            int queue = 0;
            for (int j = 0; j < vegeProto.MiningItem.Length; j++)
            {
                if ((float)dotNet35Random.NextDouble() < vegeProto.MiningChance[j])
                {
                    int itemId = vegeProto.MiningItem[j];
                    int itemCount = (int)(vegeProto.MiningCount[j] * (vegeData.scl.y * vegeData.scl.y) + 0.5f);
                    if (itemCount > 0 && LDB.items.Select(itemId) != null)
                    {
                        int addedCount = GameMain.mainPlayer.TryAddItemToPackage(itemId, itemCount, 0, true, 0, false);
                        GameMain.statistics.production.factoryStatPool[factory.index].AddProductionToTotalArray(itemId, itemCount);
                        GameMain.data.history.AddFeatureValue(2150000 + itemId, itemCount);
                        if (addedCount > 0)
                        {
                            UIItemup.Up(itemId, addedCount);
                            UIRealtimeTip.PopupItemGet(itemId, addedCount, vegeData.pos + vegeData.pos.normalized, queue++);
                        }
                    }
                }
            }
        }



        // Reference: TestSkillCasts

        static int StartLaser(PlanetFactory factory, int vegeId, Vector3 beginPos)
        {
            ref LocalLaserContinuous ptr = ref GameMain.data.spaceSector.skillSystem.localLaserContinuous.Add();
            ref VegeData vegeData = ref factory.vegePool[vegeId];
            ptr.Start();
            ptr.astroId = factory.planetId;
            ptr.hitIndex = 4;
            ptr.beginPos = vegeData.pos + vegeData.pos.normalized * SkillSystem.RoughHeightByModelIndex[vegeData.modelIndex] * 0.5f;
            ptr.endPos = beginPos;
            ptr.target.type = ETargetType.Vegetable;
            ptr.target.id = vegeId;

            int maxHp = SkillSystem.HpMaxByModelIndex[vegeData.modelIndex];
            int recoverHp = SkillSystem.HpRecoverByModelIndex[vegeData.modelIndex];
            ptr.damage = (int)((maxHp / tickToClear + recoverHp) / 1.0f + 1); // Kill the tree/stones after MiningTick 
            ptr.damageScale = 1.0f; // TickSkillLogic: vfaudio.volumeMultiplier = Mathf.Min(1.5f, this.damageScale * 0.6f);
            ptr.mask = ETargetTypeMask.NotPlayer;

            // Replace laser sound with mining sound (123)
            ref SkillSFXHolder ptr2 = ref GameMain.data.spaceSector.skillSystem.audio.AddPlanetAudio(123, 0f, ptr.astroId, ptr.beginPos);
            ptr.sfxId = ptr2.id;
            return ptr.id;
        }

        static bool ContinueLaser(PlanetFactory factory, int laserId, Vector3 beginPos, out int vegeId)
        {
            ref LocalLaserContinuous ptr = ref GameMain.data.spaceSector.skillSystem.localLaserContinuous.buffer[laserId];
            ptr.endPos = beginPos;
            SkillTargetLocal target = ptr.target;
            vegeId = target.id;
            ref VegeData vegeData = ref factory.vegePool[vegeId];
            if (vegeData.id == 0)
            {
                ptr.Stop(GameMain.data.spaceSector.skillSystem);
                return false;
            }
            ptr.beginPos = vegeData.pos + vegeData.pos.normalized * SkillSystem.RoughHeightByModelIndex[vegeData.modelIndex] * 0.5f;
            if (Vector3.SqrMagnitude(ptr.endPos - beginPos) > Range * Range)
            {
                ptr.Stop(GameMain.data.spaceSector.skillSystem);
                return false;
            }
            return true;
        }

        static void StopAll()
        {
            ref var pool = ref GameMain.data.spaceSector.skillSystem.localLaserContinuous;
            for (int k = 1; k < pool.cursor; k++)
            {
                if (pool.buffer[k].id > 0)
                {
                    pool.buffer[k].Stop(GameMain.data.spaceSector.skillSystem);
                }
            }
        }

        static int GetClosestVegeId(PlanetFactory factory, Vector3 centerPos, float range)
        {
            int result = 0;
            VegeData[] vegePool = factory.vegePool;
            float minDist = range * range;

            HashSystem hashSystemStatic = factory.hashSystemStatic; // The unmoveable objects
            hashSystemStatic.ClearActiveBuckets();
            int[] hashPool = hashSystemStatic.hashPool;
            int[] bucketOffsets = hashSystemStatic.bucketOffsets;
            int[] bucketCursors = hashSystemStatic.bucketCursors;
            hashSystemStatic.GetBucketIdxesInArea(centerPos, range);
            int[] activeBuckets = hashSystemStatic.activeBuckets;
            int activeBucketsCount = hashSystemStatic.activeBucketsCount;
            for (int k = 0; k < activeBucketsCount; k++)
            {
                int bucketId = activeBuckets[k];
                int offset = bucketOffsets[bucketId];
                int end = offset + bucketCursors[bucketId];
                for (int l = offset; l < end; l++)
                {
                    int hashAddress = hashPool[l];
                    if (hashAddress != 0 && hashAddress >> 28 == 1) // ETargetType.Vegetable = 1
                    {
                        int vegeId = hashAddress & 268435455;
                        if (vegeId != 0)
                        {
                            if (checkVeges.TryGetValue(vegeId, out bool state)) // Vege is in cache
                            {
                                if (state)
                                {
                                    ref VegeData vege = ref vegePool[vegeId];
                                    float dist = Vector3.SqrMagnitude(vege.pos - centerPos);
                                    if (dist < minDist)
                                    {
                                        result = vegeId;
                                        minDist = dist;
                                    }
                                }
                            }
                            else
                            {
                                // Check if the new vege is available target
                                ref VegeData vege = ref vegePool[vegeId];
                                VegeProto vegeProto = LDB.veges.Select(vege.protoId);
                                if (vege.id == vegeId && vegeProto != null)
                                {
                                    if (DropOnly && vegeProto.MiningItem.Length == 0)
                                    {
                                        checkVeges[vegeId] = false;
                                        continue;
                                    }
                                    if (vege.protoId == 9999 && !SpaceCapsule) // Found in NotifyOnVegetableMined
                                    {
                                        checkVeges[vegeId] = false;
                                        continue;
                                    }
                                    float dist = Vector3.SqrMagnitude(vege.pos - centerPos);
                                    if (dist < minDist)
                                    {
                                        result = vegeId;
                                        minDist = dist;
                                    }
                                    checkVeges[vegeId] = true;
                                    continue;
                                }
                                checkVeges[vegeId] = false;
                            }
                        }
                    }
                }
            }
            hashSystemStatic.ClearActiveBuckets();
            return result;
        }
    }
}
