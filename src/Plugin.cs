using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(LaserClearing.Plugin.NAME)]
[assembly: AssemblyProduct(LaserClearing.Plugin.GUID)]
[assembly: AssemblyVersion(LaserClearing.Plugin.VERSION)]
[assembly: AssemblyFileVersion(LaserClearing.Plugin.VERSION)]

namespace LaserClearing
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.LaserClearing";
        public const string NAME = "LaserClearing";
        public const string VERSION = "1.0.2";

        public static Plugin Instance;
        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Instance = this;
            Log = Logger;
            harmony = new Harmony(GUID);
            LoadConfigs();
            harmony.PatchAll(typeof(LocalLaser_Patch));
            harmony.PatchAll(typeof(UI_Patch));
#if DEBUG
            UI_Patch.OnEnableChanged();
#endif
        }

#if DEBUG
        public void OnDestroy()
        {
            LocalLaser_Patch.ClearAll();
            UI_Patch.OnDestory();
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif

        public static void LoadConfigs()
        {
            LocalLaser_Patch.Enable = Instance.Config.Bind("General", "Enable", true, "Enable LaserClearing").Value;
            LocalLaser_Patch.EnableLoot = Instance.Config.Bind("General", "EnableLoot", true, "Get drops from destroying trees and stones\n破坏树木/石头时获取掉落物").Value;
            LocalLaser_Patch.RequiredSpace = Instance.Config.Bind("General", "RequiredSpace", 2, "Stop laser when there is not enough space in inventory\n物品栏保留空位,当空间不足时停止激光").Value;
            LocalLaser_Patch.MaxLaserCount = Instance.Config.Bind("Laser", "MaxCount", 3, "Maximum count of laser\n激光最大数量").Value;
            LocalLaser_Patch.Range = Instance.Config.Bind("Laser", "Range", 40f, "Maximum range of laser\n激光最远距离").Value;
            LocalLaser_Patch.MiningTick = Instance.Config.Bind("Laser", "MiningTick", 60, "Time to mine an object (tick)\n开采所需时间").Value;
            LocalLaser_Patch.CheckIntervalTick = Instance.Config.Bind("Laser", "CheckIntervalTick", 20, "Interval to check objects in range\n检查周期").Value;
            LocalLaser_Patch.MiningPower = Instance.Config.Bind("Laser", "MiningPower", 480f, "Power consumption per laser (kW)\n激光耗能").Value / 60f * 1000f; // Vanilla: 640kW
            LocalLaser_Patch.DropOnly = Instance.Config.Bind("Target", "DropOnly", true, "Targets only objects with available drop\n只清除有掉落物的植被").Value;
            LocalLaser_Patch.SpaceCapsule = Instance.Config.Bind("Target", "SpaceCapsule", false, "Targets space capsule\n清除飞行仓").Value;
            Instance.Logger.LogDebug($"LoadConfigs drop:{LocalLaser_Patch.DropOnly}");
        }
    }
}
