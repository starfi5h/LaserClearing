using HarmonyLib;

namespace LaserClearing
{
    public static class Compatibility
    {
        private static OnMineDelegate _onMine;

        public delegate void OnMineDelegate(
            ref PlanetFactory factory,
            int id
        );

        public static void Start(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos
                .ContainsKey("org.ProfessorCat305.OrbitalRing"))
            {
                return;
            }

            var method = AccessTools.Method("ProjectOrbitalRing.Patches.Logic.FelTreeObjects:OnMine");

            if (method != null) {
                _onMine = AccessTools.MethodDelegate<OnMineDelegate>(method);
                harmony.PatchAll(typeof(Compatibility));
            }
            else
            {
                Plugin.Log.LogWarning("Error when patching OrbitalRing! Last target version: 1.0.20");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalLaser_Patch), "GetVegLoot")]
        static void GetVegLoot_Prefix(PlanetFactory factory, in VegeData vegeData)
        {
            // https://github.com/ProfessorCat305/OrbitalRing-MOD/blob/main/src/Patches/Logic/FelTreeObjects.cs
            // 除了原版採集獎勵外, 額外增加mod採集獎勵
            _onMine(ref factory, vegeData.id);
        }
    }
}
