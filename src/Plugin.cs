using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

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

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
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
    }
}
