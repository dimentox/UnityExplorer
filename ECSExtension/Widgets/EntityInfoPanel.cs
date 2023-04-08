using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodules.ModComponent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace ECSExtension.Widgets
{
    public class EntityInfoPanel
    {
        public EntityInspector Owner { get; }
        private Entity Target => Owner.currentEntity;

        private string lastName;
        private int lastWorld;

        private Text titleLabel;
        private InputFieldRef NameInput;
        private Toggle ActiveSelfToggle;
        private Text ActiveSelfText;

        private ButtonRef WorldButton;
        private Dropdown WorldDropdown;

        public EntityInfoPanel(EntityInspector owner)
        {
            Owner = owner;
            Create();
        }

        public void UpdateEntityInfo(bool firstUpdate, bool force)
        {
            if (force || (!NameInput.Component.isFocused && GetEntityName() != lastName))
            {
                lastName = GetEntityName();
                Owner.Tab.TabText.text = $"[G] {GetEntityName()}";
                NameInput.Text = GetEntityName();
            }
            
            if (force || IsEntityEnabled() != ActiveSelfToggle.isOn)
            {
                ActiveSelfToggle.Set(IsEntityEnabled(), false);
                ActiveSelfText.color = ActiveSelfToggle.isOn ? Color.green : Color.red;
            }

            if (force || (Owner.currentWorldIndex != lastWorld))
            {
                lastWorld = Owner.currentWorldIndex;
                WorldDropdown.value = Owner.currentWorldIndex;
                WorldButton.ButtonText.text = Owner.currentWorld.Name;
            }

            titleLabel.text = Owner.GetEntityName();

            if (worldNames == null || firstUpdate)
                GetWorldNames();
            WorldDropdown.ClearOptions();
            foreach (string name in worldNames)
                WorldDropdown.options.Add(new Dropdown.OptionData(name));
            WorldDropdown.value = Owner.currentWorldIndex;
            WorldDropdown.RefreshShownValue();
        }

        public string GetEntityName()
        {
            Owner.entityManager.GetName(Target, out FixedString64Bytes fixedString);
            return fixedString.Value;
        }

        public bool IsEntityEnabled()
        {
            return !Owner.entityManager.HasComponent<Disabled>(Target);
        }
        
        #region UI event listeners

        void OnNameEndEdit(string value)
        {
            Owner.entityManager.SetName(Target, value);
            UpdateEntityInfo(false, true);
        }

        void OnCopyClicked()
        {
            ClipboardPanel.Copy(Target);
        }
        
        public void SetEnabled(bool enabled)
        {
            EntityManager entityManager = Owner.entityManager;
            if (IsEntityEnabled() == enabled)
            {
                return;
            }
            ComponentType componentType = ComponentType.ReadWrite<Disabled>();
            if (entityManager.HasModComponent<LinkedEntityGroup>(Target))
            {
                NativeArray<Entity> entities = Reinterpret<LinkedEntityGroup, Entity>(entityManager.GetBuffer<LinkedEntityGroup>(Target)).ToNativeArray(Allocator.TempJob);
                if (enabled)
                {
                    entityManager.RemoveComponent(entities, componentType);
                }
                else
                {
                    entityManager.AddComponent(entities, componentType);
                }
                entities.Dispose();
                return;
            }
            if (!enabled)
            {
                entityManager.AddComponent(Target, componentType);
                return;
            }
            entityManager.RemoveComponent(Target, componentType);
        }
        
        public unsafe DynamicBuffer<U> Reinterpret<T, U>(DynamicBuffer<T> buffer) 
            where U : unmanaged
            where T : unmanaged
        {
            return new DynamicBuffer<U>(buffer.m_Buffer, buffer.m_InternalCapacity);
        }

        void OnActiveSelfToggled(bool value)
        {
            SetEnabled(value);
            UpdateEntityInfo(false, true);
        }

        void OnWorldButtonClicked()
        {
            InspectorManager.Inspect(Owner.currentWorld);
        }

        void OnWorldDropdownChanged(int value)
        {
            Owner.SetWorld(value);
            UpdateEntityInfo(false, true);
        }

        void OnDestroyClicked()
        {
            Owner.entityManager.DestroyEntity(Target);
            Owner.CloseInspector();
        }

        void OnInstantiateClicked()
        {
            ExplorerCore.Log("Not Implemented!");
        }
        

        #endregion


        #region UI Construction

        public void Create()
        {
            GameObject topInfoHolder = UIFactory.CreateVerticalGroup(Owner.Content, "TopInfoHolder", false, false, true, true, 3,
                new Vector4(3, 3, 3, 3), new Color(0.1f, 0.1f, 0.1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topInfoHolder, minHeight: 100, flexibleWidth: 9999);
            topInfoHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title and update row

            GameObject titleRow = UIFactory.CreateUIObject("TitleRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleRow, false, false, true, true, 5);

            titleLabel = UIFactory.CreateLabel(titleRow, "Title", Owner.GetEntityName(), fontSize: 17);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, minWidth: 100);

            // name

            NameInput = UIFactory.CreateInputField(titleRow, "NameInput", "untitled");
            UIFactory.SetLayoutElement(NameInput.Component.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);
            NameInput.Component.textComponent.fontSize = 15;
            NameInput.Component.GetOnEndEdit().AddListener(val => { OnNameEndEdit(val); });

            // second row (toggles, instanceID, tag, buttons)

            GameObject secondRow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(secondRow, false, false, true, true, 5, 0, 0, 0, 0);
            UIFactory.SetLayoutElement(secondRow, minHeight: 25, flexibleWidth: 9999);

            // activeSelf
            GameObject activeToggleObj = UIFactory.CreateToggle(secondRow, "ActiveSelf", out ActiveSelfToggle, out ActiveSelfText);
            UIFactory.SetLayoutElement(activeToggleObj, minHeight: 25, minWidth: 100);
            ActiveSelfText.text = "ActiveSelf";
            ActiveSelfToggle.onValueChanged.AddListener(OnActiveSelfToggled);

            GameObject spacer = UIFactory.CreateUIObject("Spacer", secondRow);
            UIFactory.SetLayoutElement(spacer, minWidth: 25, preferredWidth: 9999);

            // Instantiate
            ButtonRef instantiateBtn = UIFactory.CreateButton(secondRow, "InstantiateBtn", "Instantiate", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(instantiateBtn.Component.gameObject, minHeight: 25, minWidth: 120);
            instantiateBtn.OnClick += OnInstantiateClicked;

            // Destroy
            ButtonRef destroyBtn = UIFactory.CreateButton(secondRow, "DestroyBtn", "Destroy", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(destroyBtn.Component.gameObject, minHeight: 25, minWidth: 80);
            destroyBtn.OnClick += OnDestroyClicked;

            // third row (scene, layer, flags)

            GameObject thirdrow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(thirdrow, false, false, true, true, 5, 0, 0, 0, 0);
            UIFactory.SetLayoutElement(thirdrow, minHeight: 25, flexibleWidth: 9999);

            // Scene
            Text sceneLabel = UIFactory.CreateLabel(thirdrow, "SceneLabel", "Current World:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(sceneLabel.gameObject, minHeight: 25, minWidth: 100);

            WorldButton = UIFactory.CreateButton(thirdrow, "SceneButton", "untitled");
            UIFactory.SetLayoutElement(WorldButton.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 999);
            WorldButton.OnClick += OnWorldButtonClicked;

            // Layer
            Text layerLabel = UIFactory.CreateLabel(thirdrow, "LayerLabel", "Worlds:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(layerLabel.gameObject, minHeight: 25, minWidth: 50);

            GameObject layerDrop = UIFactory.CreateDropdown(thirdrow, "LayerDropdown", out WorldDropdown, "0", 14, OnWorldDropdownChanged);
            UIFactory.SetLayoutElement(layerDrop, minHeight: 25, minWidth: 150, flexibleWidth: 999);

            GameObject spacer1 = UIFactory.CreateUIObject("Spacer", thirdrow);
            UIFactory.SetLayoutElement(spacer1, minWidth: 25, preferredWidth: 9999);
        }

        private List<string> worldNames;

        private void GetWorldNames()
        {
            worldNames = Owner.validWorlds
                .Select(world => world.Name).ToList();
        }

        #endregion
    }
}