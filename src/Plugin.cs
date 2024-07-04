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
        public const string VERSION = "1.0.5";

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
            LocalLaser_Patch.Enable = Instance.Config.Bind("General", "Enable", false, "Enable LaserClearing when starting the game\n进入游戏时启用激光").Value;
            LocalLaser_Patch.EnableLoot = Instance.Config.Bind("General", "EnableLoot", true, "Get drops from destroying trees and stones when enable laser\n启用激光时,破坏树木/石头时会获取掉落物").Value;
            LocalLaser_Patch.RequiredSpace = Instance.Config.Bind("General", "RequiredSpace", 5, "Stop laser when there is not enough space in inventory\n物品栏保留空位,当空间不足时停止激光").Value;
            LocalLaser_Patch.MaxLaserCount = Instance.Config.Bind("Laser", "MaxCount", 3, "Maximum count of laser\n激光最大数量").Value;
            LocalLaser_Patch.Range = Instance.Config.Bind("Laser", "Range", 40f, "Maximum range of laser\n激光最远距离").Value;
            LocalLaser_Patch.MiningTick = Instance.Config.Bind("Laser", "MiningTick", 90, "Time to mine an object (tick)\n开采所需时间").Value;
            //LocalLaser_Patch.CheckIntervalTick = Instance.Config.Bind("Laser", "CheckIntervalTick", 20, "Interval to check objects in range (laser cool-down time)\n检查周期(激光冷却时间)").Value;
            LocalLaser_Patch.MiningPower = Instance.Config.Bind("Laser", "MiningPower", 480f, "Power consumption per laser (kW)\n激光耗能").Value / 60f * 1000f; // Vanilla: 640kW
            LocalLaser_Patch.DropOnly = Instance.Config.Bind("Target", "DropOnly", true, "Targets only objects with available drop\n只清除有掉落物的植被").Value;
            LocalLaser_Patch.SpaceCapsule = Instance.Config.Bind("Target", "SpaceCapsule", false, "Targets space capsule\n清除飞行仓").Value;
            
            LocalLaser_Patch.EnableDestructionSFX = Instance.Config.Bind("Other", "EnableDestructionSFX", false, "Play sounds when trees and stones get clear by laser\n激光清除树木/石头时播放音效").Value;
            LocalLaser_Patch.ScaleWithDroneCount = Instance.Config.Bind("Other", "ScaleWithDroneCount", 0.0f, "Scale laser count with (this ratio * consturction drone count)\n>0时, 使激光数量随(科技无人机数量*此值)增长").Value;
            LocalLaser_Patch.ScaleWithMiningSpeed = Instance.Config.Bind("Other", "ScaleWithMiningSpeed", 0.0f, "Scale mining tick with (this ratio * mining speed boost)\n>0时, 使激光效率随(科技矿物采集速度*此值)增长").Value;

            Instance.Logger.LogDebug($"LoadConfigs drop:{LocalLaser_Patch.DropOnly}");
        }
    }
}
