using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LaserClearing
{
    public class UI_Patch
    {
		static UIButton enableButton;

		[HarmonyPostfix, HarmonyPatch(typeof(UIMechaEnergy), nameof(UIMechaEnergy._OnCreate))]
		public static void OnEnableChanged()
		{
			if (enableButton == null)
            {
				var infiniteEnergyButton = UIRoot.instance.uiGame.energyBar.infiniteEnergyButton;
				var gameObject = GameObject.Instantiate(infiniteEnergyButton.gameObject, infiniteEnergyButton.transform.parent);
				gameObject.name = "[LaserClearing] Toggle";
				gameObject.transform.localPosition += new Vector3(30, 0, 0);
				gameObject.SetActive(true);
				var image = gameObject.transform.Find("icon").GetComponent<Image>();
				var treeIcon = UIRoot.instance.uiGame.sandboxMenu.categoryIcons[3];
				image.sprite = treeIcon.sprite;

				enableButton = gameObject.GetComponent<UIButton>();
                enableButton.onClick += EnableButton_onClick;
				enableButton.tips.tipTitle = "LaserClearing";
				enableButton.tips.tipText = "Enable/Disable mecha laser to clear trees and stones";
				enableButton.tips.corner = 8;		
				enableButton.transitions[0].highlightColorOverride = new Color(0.6f, 0.6f, 0.6f, 0.1f); // button background
				enableButton.transitions[1].highlightColorOverride = new Color(0.6f, 0.6f, 1.0f, 1.0f); // icon
			}
			enableButton.highlighted = LocalLaser_Patch.Enable;
		}

        private static void EnableButton_onClick(int obj)
        {
			LocalLaser_Patch.Enable = !LocalLaser_Patch.Enable;
			enableButton.highlighted = LocalLaser_Patch.Enable;
			if (!LocalLaser_Patch.Enable)
				LocalLaser_Patch.ClearAll();
		}

        public static void OnDestory()
        {
			GameObject.Destroy(enableButton?.gameObject);
        }
    }
}
