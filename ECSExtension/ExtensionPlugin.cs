using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ECSExtension.Panels;
using ECSExtension.Patch;
using ECSExtension.Util;
using HarmonyLib;
using Unity.Entities;
using UnityExplorer;
using UnityExplorer.Inspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace ECSExtension
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, VERSION)]
    [BepInDependency(ExplorerCore.GUID)]
    public class ExtensionPlugin : BasePlugin
    {
        public const string PLUGIN_NAME = "ECS Inspector Extension";

        public const string PLUGIN_GUID = "org.kremnev8.plugin.ecs-inspector-extension";

        public const string VERSION = "1.0.2";

        public static Harmony Harmony;

        public static ManualLogSource logger;
        
        public override void Load()
        {
            logger = Log;
            Harmony = new Harmony(PLUGIN_GUID);
            Harmony.PatchAll(typeof(GameObjectConversionMappingSystem_Patch));
            
            ECSUtil.Init();
            
            InspectorManager.customInspectors.Add(EntityAdder);
            InspectorManager.equalityCheckers.Add(typeof(Entity), EntityEqualityChecker);
            UIManager.onInit += UIManagerOnInit;
            Log.LogInfo("Added Entity Inspector");
        }

        private void UIManagerOnInit()
        {
            ObjectExplorerPanel explorerPanel = UIManager.GetPanel<ObjectExplorerPanel>(UIManager.Panels.ObjectExplorer);
            explorerPanel.AddTab(new WorldExplorer(explorerPanel));
            Log.LogInfo("Added World Explorer");
        }

        private bool EntityEqualityChecker(object o1, object o2)
        {
            if (o1 is Entity e1 && o2 is Entity e2)
            {
                return e1.Equals(e2);
            }

            return false;
        }

        private Type EntityAdder(object o)
        {
            if (o is Entity)
            {
                return typeof(EntityInspector);
            }

            return null;
        }

        public override bool Unload()
        {
            Harmony.UnpatchSelf();
            List<InspectorBase> entityInspectors = new List<InspectorBase>();
            foreach (InspectorBase inspector in InspectorManager.Inspectors)
            {
                if (inspector is EntityInspector)
                {
                    entityInspectors.Add(inspector);
                }
            }

            foreach (InspectorBase inspector in entityInspectors)
            {
                inspector.CloseInspector();
            }
            
            InspectorManager.customInspectors.Remove(EntityAdder);
            InspectorManager.equalityCheckers.Remove(typeof(Entity));
            Log.LogInfo("Removed Entity Inspector");
            return true;
        }
    }
}