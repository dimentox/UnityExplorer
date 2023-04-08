using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.Inspectors;
using UniverseLib;
using UniverseLib.UI.Widgets.ButtonList;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;
using Type = Il2CppSystem.Type;

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

            cell.OnBehaviourToggled += OnBehaviourToggled;
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
            System.Type monoType = Il2CppReflection.GetUnhollowedType(componentType);

            var method = typeof(ECSComponentList).GetMethod(methodName, AccessTools.all);
            method.MakeGenericMethod(monoType)
                .Invoke(this, Array.Empty<object>());
        }

        private void InspectComponent<T>() where T : unmanaged
        {
            CacheComponent<T> data = Parent.GetComponentData<T>();
            InspectorManager.Inspect(data.TryEvaluate(), data);
        }

        private void OnBehaviourToggled(bool value, int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];

             //   if (comp.TryCast<Behaviour>() is Behaviour behaviour)
            //        behaviour.enabled = value;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception toggling Behaviour.enabled: {ex.ReflectionExToString()}");
            }
        }

        private void OnDestroyClicked(int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];
                
                InvokeForComponent(comp, nameof(RemoveComponent));

                Parent.UpdateComponents();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception destroying Component: {ex.ReflectionExToString()}");
            }
        }

        private void RemoveComponent<T>() where T : unmanaged
        {
            Parent.RemoveComponent<T>();
        }

        private static readonly Dictionary<string, string> compToStringCache = new Dictionary<string, string>();

        // Called from ButtonListHandler.SetCell, will be valid
        private void SetComponentCell(ECSComponentCell cell, int index)
        {
            var entries = GetEntries();
            cell.Enable();

            try
            {
                var comp = entries[index];
                Type type = comp.GetManagedType();

               // if (!compToStringCache.ContainsKey(type.AssemblyQualifiedName))
              //      compToStringCache.Add(type.AssemblyQualifiedName, SignatureHighlighter.Parse(type, true));

              cell.Button.ButtonText.text = type.ToString(); //compToStringCache[type.AssemblyQualifiedName];
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Error setting component name: {e.Message}, stacktrace: {e.StackTrace}");
            }

           /* // if component is the first index it must be the transform, dont show Destroy button for it.
            if (index == 0 && cell.DestroyButton.Component.gameObject.activeSelf)
                cell.DestroyButton.Component.gameObject.SetActive(false);
            else if (index > 0 && !cell.DestroyButton.Component.gameObject.activeSelf)
                cell.DestroyButton.Component.gameObject.SetActive(true);*/
        }
    }
}