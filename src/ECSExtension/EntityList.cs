using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.Inspectors;
using UniverseLib;
using UniverseLib.UI.Widgets.ButtonList;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace ECSExtension
{
    public class EntityList : ButtonListHandler<Entity, EntityCell>
    {
        public EntityInspector Parent;

        public EntityList(ScrollPool<EntityCell> scrollPool, Func<List<Entity>> getEntriesMethod)
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

        private bool CheckShouldDisplay(Entity _, string __) => true;

        public override void OnCellBorrowed(EntityCell cell)
        {
            base.OnCellBorrowed(cell);

            cell.OnBehaviourToggled += OnBehaviourToggled;
            cell.OnDestroyClicked += OnDestroyClicked;
        }

        private void OnComponentClicked(int index)
        {
            var entries = GetEntries();

            if (index < 0 || index >= entries.Count)
                return;

            Entity comp = entries[index];
            if (comp != Entity.Null)
                Parent.InspectEntity(comp);
        }

        private void OnBehaviourToggled(bool value, int index)
        {
            try
            {
                var entries = GetEntries();
                var comp = entries[index];
                //TODO toggle entities

                //if (comp.TryCast<Behaviour>() is Behaviour behaviour)
                //    behaviour.enabled = value;
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

                //TODO remove entities
                // GameObject.DestroyImmediate(comp);

                Parent.UpdateComponents();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception destroying Component: {ex.ReflectionExToString()}");
            }
        }

        private static readonly Dictionary<string, string> compToStringCache = new Dictionary<string, string>();

        // Called from ButtonListHandler.SetCell, will be valid
        private void SetComponentCell(EntityCell cell, int index)
        {
            var entries = GetEntries();
            cell.Enable();
            var comp = entries[index];
            
            var type = comp.GetActualType();

            //if (!compToStringCache.ContainsKey(type.AssemblyQualifiedName))
           //     compToStringCache.Add(type.AssemblyQualifiedName, SignatureHighlighter.Parse(type, true));

            cell.Button.ButtonText.text = comp.ToString();

            // if component is the first index it must be the transform, dont show Destroy button for it.
           /* if (index == 0 && cell.DestroyButton.Component.gameObject.activeSelf)
                cell.DestroyButton.Component.gameObject.SetActive(false);
            else if (index > 0 && !cell.DestroyButton.Component.gameObject.activeSelf)
                cell.DestroyButton.Component.gameObject.SetActive(true);*/
        }
    }
}