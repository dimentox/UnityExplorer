using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Unity.Entities;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets.ScrollView;

namespace ECSExtension.Panels
{
    public class QueryComponentList : ICellPoolDataSource<QueryComponentCell>
    {
        private List<string> componentNames = new List<string>();
        private ScrollPool<QueryComponentCell> scrollPool;
        public int ItemCount => componentNames.Count + 1;
        private LayoutElement viewportLayout;
        
        public QueryComponentList(ScrollPool<QueryComponentCell> scrollPool, LayoutElement viewportLayout)
        {
            this.scrollPool = scrollPool;
            this.viewportLayout = viewportLayout;
            scrollPool.Initialize(this);
            var sliderContainer = this.scrollPool.UIRoot.transform.FindChild("SliderContainer").gameObject;
            sliderContainer.SetActive(false);
            scrollPool.Refresh(true, true);
        }

        public ComponentType[] GetComponentTypes()
        {
            return componentNames
                .Select(ReflectionUtility.GetTypeByName)
                .Where(type => type != null)
                .Select(type =>
                {
                    var il2cppType = Il2CppType.From(type);
                    return TypeManager.GetTypeIndex(il2cppType);
                })
                .Where(index => index >= 0)
                .Select(ComponentType.FromTypeIndex).ToArray();
        }
        
        public void OnCellBorrowed(QueryComponentCell cell)
        {
            cell.OnTextChanged += OnTextChanged;
        }

        private void OnTextChanged(int index, string text)
        {
            if (index >= 0 && index < componentNames.Count)
            {
                if (string.IsNullOrEmpty(text))
                    componentNames.RemoveAt(index);
                else
                    componentNames[index] = text;
            }
            else
            {
                componentNames.Add(text);
            }

            viewportLayout.preferredHeight = ItemCount * 25;
            scrollPool.Refresh(true, true);
        }

        public void SetCell(QueryComponentCell cell, int index)
        {
            if (index >= 0 && index < componentNames.Count)
                cell.ConfigureCell(index, componentNames[index]);
            else if (index < componentNames.Count + 1)
                cell.ConfigureCell(index, "");
            else
                cell.Disable();
        }
    }
}