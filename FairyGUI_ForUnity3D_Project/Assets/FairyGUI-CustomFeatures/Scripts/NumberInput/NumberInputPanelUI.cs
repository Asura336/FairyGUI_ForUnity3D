using FairyGUI;
using FairyGUI.Extensions;
using UnityEngine;

namespace Scripts
{
    [RequireComponent(typeof(UIPanel))]
    public class NumberInputPanelUI : MonoBehaviour
    { 
        private void Awake()
        {
            UIPackage.AddPackage("UI/CustomFeatures");
            UIObjectFactory.SetPackageItemExtension("ui://CustomFeatures/NumberInputButton",
                typeof(NumberInputButton));
        }

        private void Start()
        {
           var ui = GetComponent<UIPanel>().ui;

            var integerInput = ui.GetChild("integer") as NumberInputButton;
            integerInput.Value = 0;

            var floatInput = ui.GetChild("float") as NumberInputButton;
            floatInput.CustomOptions = new NumberInputUserMessage
            {
                dx = 0.001f,
                integer = false,
                roundDigits = 6,
            };
            floatInput.min = -100;
            floatInput.Value = 0;
        }

    }
}