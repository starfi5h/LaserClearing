using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LaserClearing
{
    public class LocalLaser_Patch
    {
		public static int MaxLaserCount = 3;
		public static float Range = 40f;

		static HashSet<int> miningVegeIds = new();
		static List<int> laserIds = new();

		[HarmonyPostfix, HarmonyPatch(typeof(PlayerAction_Mine), nameof(PlayerAction_Mine.GameTick))]
		static void GameTick(PlayerAction_Mine __instance)
        {
			Vector3 beginPos = __instance.player.mecha.skillCastRightL;
			PlanetFactory factory = __instance.player.factory;

			if (laserIds.Count < MaxLaserCount)
			{
				int vegeId = GetClosestVegeId(factory, beginPos, Range);
				if (vegeId != 0)
                {
					miningVegeIds.Add(vegeId);
					laserIds.Add(StartLaser(factory, vegeId, beginPos));
				}
			}
			for (int i = laserIds.Count - 1; i >= 0; i--)
            {
				int removeTargetId = ContinueLaser(factory, laserIds[i], beginPos, Range);
				if (removeTargetId != 0)
                {
					miningVegeIds.Remove(removeTargetId);
					laserIds.RemoveAt(i);
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
		[HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
		public static void ClearAll()
        {
			miningVegeIds.Clear();
			laserIds.Clear();
			StopAll();
		}

		// Reference: TestSkillCasts

		static int StartLaser(PlanetFactory factory, int vegeId, Vector3 beginPos)
        {
			ref LocalLaserContinuous ptr = ref GameMain.data.spaceSector.skillSystem.localLaserContinuous.Add();
			ref VegeData vegeData = ref factory.vegePool[vegeId];
			ptr.Start();
			ptr.astroId = factory.planetId;
			ptr.hitIndex = 4;
			ptr.beginPos = beginPos;
			ptr.endPos = vegeData.pos + vegeData.pos.normalized * SkillSystem.RoughHeightByModelIndex[vegeData.modelIndex] * 0.5f;
			ptr.target.type = ETargetType.Vegetable;
			ptr.target.id = vegeId;
			ptr.damage = 0; // Dummy damage as only visual effects
			ptr.damageScale = 0.1f; // TickSkillLogic: vfaudio.volumeMultiplier = Mathf.Min(1.5f, this.damageScale * 0.6f);
			ptr.mask = ETargetTypeMask.NotPlayer;
			return ptr.id;
		}

		static int ContinueLaser(PlanetFactory factory, int laserId,  Vector3 beginPos, float rangeLimit)
        {
			ref LocalLaserContinuous ptr = ref GameMain.data.spaceSector.skillSystem.localLaserContinuous.buffer[laserId];
			ptr.beginPos = beginPos;
			SkillTargetLocal target = ptr.target;
			ref VegeData vegeData = ref factory.vegePool[target.id];
			if (vegeData.id == 0)
            {
				ptr.Stop(GameMain.data.spaceSector.skillSystem);
				return target.id;
			}
			//ptr.endPos = vegeData.pos + vegeData.pos.normalized * SkillSystem.RoughHeightByModelIndex[vegeData.modelIndex] * 0.5f;
			ptr.endPos = vegeData.pos;
			if (Vector3.SqrMagnitude(ptr.endPos - beginPos) > rangeLimit * rangeLimit)
            {
				ptr.Stop(GameMain.data.spaceSector.skillSystem);
				return target.id;
			}
			return 0;
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
						if (vegeId != 0 && !miningVegeIds.Contains(vegeId))
						{
							ref VegeData vege = ref vegePool[vegeId];
							if (vege.id == vegeId)
							{
								float dist = Vector3.SqrMagnitude(vege.pos - centerPos);
								if (dist < minDist)
								{
									result = vegeId;
									minDist = dist;
								}
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
