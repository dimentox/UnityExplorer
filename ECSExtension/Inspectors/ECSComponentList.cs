using System;
using System.Collections.Generic;
using ECSExtension.Util;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using UnityExplorer;
using UniverseLib;
using UniverseLib.Runtime;
using UniverseLib.UI.Widgets.ScrollView;

#if CPP
using Type = Il2CppSystem.Type;
#endif

namespace ECSExtension
{
    public class ECSComponentList : ButtonNativeListHandler<ComponentType, ECSComponentCell>
    {
        public EntityInspector Parent;

        public ECSComponentList(ScrollPool<ECSComponentCell> scrollPool, Func<NativeArray<ComponentType>> getEntriesMethod)
            : base(scrollPool, getEntriesMethod, null, null, null)
        {
            SetICell = SetComponentCell;
            ShouldDisplay = CheckShouldDisplay;
            OnCellClicked = OnComponentClicked;
        }

        public void Clear()
        {
            RefreshData();
            ScrollPool.Refresh(true, true);
        }

        private bool CheckShouldDisplay(ComponentType _, string __) => true;

        public override void OnCellBorrowed(ECSComponentCell cell)
        {
            base.OnCellBorrowed(cell);

            cell.OnDestroyClicked += OnDestroyClicked;
        }

        private void OnComponentClicked(int index)
        {
            var entries = GetEntries();

            if (index < 0 || index >= entries.Length)
                return;

            ComponentType comp = entries[index];
            InvokeForComponent(comp, nameof(InspectComponent));
        }

        private void InvokeForComponent(ComponentType comp, string methodName)
        {
            Type componentType = comp.GetManagedType();
#if CPP
            System.Type monoType = Il2CppReflection.GetUnhollowedType(componentType);
#else
            Type monoType = componentType;
#endif
            var method = typeof(ECSComponentList).GetMethod(methodName, AccessTools.all);
            method.MakeGenericMethod(monoType)
                .Invoke(this, Array.Empty<object>());
        }

        private void InspectComponent<T>() where T : unmanaged
        {
            ComponentType type = ECSHelper.ReadOnly<T>();
            var category = ECSHelper.GetTypeInfo(type.TypeIndex).Category;

            if (category == TypeManager.TypeCategory.BufferData)
            {
                ModDynamicBuffer<T> dynamicBuffer = Parent.GetDynamicBuffer<T>();
                InspectorManager.Inspect(new BufferView<T>(dynamicBuffer));
            }
            else
            {
                CacheComponent<T> data = Parent.GetComponentData<T>();
                InspectorManager.Inspect(data.TryEvaluate(), data);
            }
        }

        private void OnDestroyClicked(int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];
                
                Parent.RemoveComponent(comp);
                Parent.UpdateComponents();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception destroying Component: {ex.ReflectionExToString()}");
            }
        }
        
        private static readonly Dictionary<string, string> compToStringCache = new Dictionary<string, string>();

        // Called from ButtonListHandler.SetCell, will be valid
        private void SetComponentCell(ECSComponentCell cell, int index)
        {
            var entries = GetEntries();
            cell.Enable();

            try
            {
                cell.ConfigureCell(entries[index]);
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Error setting component name: {e.Message}, stacktrace: {e.StackTrace}");
            }
        }
    }
}