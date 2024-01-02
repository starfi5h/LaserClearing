using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LaserClearing
{
    public class UI_Patch
    {
		static UIButton enableButton;
		static ButtonStatus buttonStatus;

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
				enableButton.tips.corner = 8;
				enableButton.tips.tipTitle = "LaserClearing";								
				enableButton.transitions[0].highlightColorOverride = new Color(0.6f, 0.6f, 0.6f, 0.1f); // button background
				UpdateButtonStatus(ButtonStatus.Normal);
			}
			enableButton.highlighted = LocalLaser_Patch.Enable;
		}

		public enum ButtonStatus
		{
			None,
			Normal,
			NotEnoughSpace
		}

		public static void UpdateButtonStatus(ButtonStatus status)
        {
			if (buttonStatus != status)
			{
				switch (status)
				{
					case ButtonStatus.Normal:
						enableButton.tips.tipText = "Enable/Disable mecha laser to clear trees and stones";
						enableButton.transitions[1].highlightColorOverride = new Color(0.6f, 0.6f, 1.0f, 1.0f); // icon color blue
						break;

					case ButtonStatus.NotEnoughSpace:
						enableButton.tips.tipText = "Not enoguh space in inventory! Requrie: " + LocalLaser_Patch.RequiredSpace;
						enableButton.transitions[1].highlightColorOverride = new Color(1.0f, 0.4f, 0.4f, 1.0f); // icon color red
						break;
				}
				enableButton.OnEnable();
				buttonStatus = status;
			}
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
