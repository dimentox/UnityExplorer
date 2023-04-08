using System;
using UnityEngine;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ButtonList;

namespace ECSExtension
{
    public class ECSComponentCell : ButtonCell
    {
        public ButtonRef DestroyButton;

        public Action<int> OnDestroyClicked;

        private void DestroyClicked()
        {
            OnDestroyClicked?.Invoke(CurrentDataIndex);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            // Add mask to button so text doesnt overlap on Close button
            //this.Button.Component.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            this.Button.ButtonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            
            // Destroy button

            DestroyButton = UIFactory.CreateButton(UIRoot, "DestroyButton", "X", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(DestroyButton.Component.gameObject, minHeight: 21, minWidth: 25);
            DestroyButton.OnClick += DestroyClicked;

            return root;
        }
    }
}