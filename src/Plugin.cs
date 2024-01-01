using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(LaserClearing.Plugin.NAME)]
[assembly: AssemblyVersion(LaserClearing.Plugin.VERSION)]

namespace LaserClearing
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.LaserClearing";
        public const string NAME = "LaserClearing";
        public const string VERSION = "1.0.0";

        public static Plugin Instance;
        static Harmony harmony;

        public void Awake()
        {
            Instance = this;
            harmony = new Harmony(GUID);
            LoadConfigs();
            harmony.PatchAll(typeof(LocalLaser_Patch));
            harmony.PatchAll(typeof(UI_Patch));
#if DEBUG
            UI_Patch.OnEnableChanged();
#endif
        }

        public void OnDestroy()
        {
            LocalLaser_Patch.ClearAll();
            UI_Patch.OnDestory();
            harmony.UnpatchSelf();
            harmony = null;
        }

        public static void LoadConfigs()
        {
            LocalLaser_Patch.Enable = Instance.Config.Bind("General", "Enable", true, "Enable LaserClearing").Value;
            LocalLaser_Patch.EnableLoot = Instance.Config.Bind("General", "EnableLoot", true, "Get drops from destroying trees and stones\n破坏的树木/石头时获取掉落物").Value;
            LocalLaser_Patch.MaxLaserCount = Instance.Config.Bind("Laser", "MaxCount", 3, "Maximum count of laser\n激光最大数量").Value;
            LocalLaser_Patch.Range = Instance.Config.Bind("Laser", "Range", 40f, "Maximum range of laser\n激光最远距离").Value;
            LocalLaser_Patch.MiningTick = Instance.Config.Bind("Laser", "MiningTick", 60, "Time to mine an object (tick)\n开采所需时间").Value;
            LocalLaser_Patch.CheckIntervalTick = Instance.Config.Bind("Laser", "CheckIntervalTick", 20, "Interval to check objects in range\n检查周期").Value;
            LocalLaser_Patch.MiningPower = Instance.Config.Bind("Laser", "MiningPower", 360f, "Power consumption  per laser (kW)\n激光耗能").Value / 60f * 100f;
            Instance.Logger.LogDebug("LoadConfigs");
        }
    }
}
