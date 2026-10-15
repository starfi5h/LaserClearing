using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
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
        public const string VERSION = "1.0.8";

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

        public void Start()
        {
            Compatibility.Start(harmony);
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
            BindLiveConfig("General", "Enable", false,
                "Enable LaserClearing when starting the game\n进入游戏时启用激光",
                v => LocalLaser_Patch.Enable = v);

            BindLiveConfig("General", "EnableLoot", true,
                "Get drops from destroying trees and stones when enable laser\n启用激光时,破坏树木/石头时会获取掉落物",
                v => LocalLaser_Patch.EnableLoot = v);

            BindLiveConfig("General", "RequiredSpace", 5,
                "Stop laser when there is not enough space in inventory\n物品栏保留空位,当空间不足时停止激光",
                v => LocalLaser_Patch.RequiredSpace = v);

            BindLiveConfig("Laser", "MaxCount", 3,
                "Maximum count of laser\n激光最大数量",
                v => { LocalLaser_Patch.MaxLaserCount = v; LocalLaser_Patch.SetParameters(); });

            BindLiveConfig("Laser", "Range", 40f,
                "Maximum range of laser\n激光最远距离",
                v => LocalLaser_Patch.Range = v);

            BindLiveConfig("Laser", "MiningTick", 90,
                "Time to mine an object (tick)\n开采所需时间",
                v => { LocalLaser_Patch.MiningTick = v; LocalLaser_Patch.SetParameters(); });

            BindLiveConfig("Laser", "MiningPower", 640f,
                "Power consumption per laser (kW)\n激光耗能(原版为640kW)",
                v => LocalLaser_Patch.MiningPower = v / 60f * 1000f);

            BindLiveConfig("Target", "DropOnly", true,
                "Targets only objects with available drop\n只清除有掉落物的植被",
                v => LocalLaser_Patch.DropOnly = v);

            BindLiveConfig("Target", "SpaceCapsule", false,
                "Targets space capsule\n清除飞行仓",
                v => LocalLaser_Patch.SpaceCapsule = v);

            BindLiveConfig("Other", "EnableDestructionSFX", false,
                "Play sounds when trees and stones get clear by laser\n激光清除树木/石头时播放音效",
                v => LocalLaser_Patch.EnableDestructionSFX = v);

            BindLiveConfig("Other", "ScaleWithDroneCount", 0.0f,
                "Scale laser count with (this ratio * construction drone count)\n>0时, 使激光数量随(科技无人机数量*此值)增长",
                v => { LocalLaser_Patch.ScaleWithDroneCount = v; LocalLaser_Patch.SetParameters(); });

            BindLiveConfig("Other", "ScaleWithMiningSpeed", 0.0f,
                "Scale mining tick with (this ratio * mining speed boost)\n>0时, 使激光效率随(科技矿物采集速度*此值)增长",
                v => { LocalLaser_Patch.ScaleWithMiningSpeed = v; LocalLaser_Patch.SetParameters(); });
        }

        private static ConfigEntry<T> BindLiveConfig<T>(
            string section,
            string key,
            T defaultValue,
            string description,
            Action<T> apply)
        {
            var entry = Instance.Config.Bind(
                section, key, defaultValue, description);

            // 套用初始設定值
            apply(entry.Value);

            // Configuration Manager 修改設定時立即更新
            entry.SettingChanged += (_, __) =>
            {
                apply(entry.Value);
                Instance.Logger.LogDebug(
                    $"Config changed: {section}.{key} = {entry.Value}");
            };

            return entry;
        }
    }
}
