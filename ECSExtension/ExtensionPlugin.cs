using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ECSExtension.Panels;
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

        public const string VERSION = "1.0.0";
        
        public override void Load()
        {
            InspectorManager.customInspectors.Add(EntityAdder);
            UIManager.onInit += UIManagerOnInit;
            Log.LogInfo("Added Entity Inspector");
        }

        private void UIManagerOnInit()
        {
            ObjectExplorerPanel explorerPanel = UIManager.GetPanel<ObjectExplorerPanel>(UIManager.Panels.ObjectExplorer);
            explorerPanel.AddTab(new WorldExplorer(explorerPanel));
            Log.LogInfo("Added World Explorer");
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
            
            InspectorManager.customInspectors.Clear();
            Log.LogInfo("Removed Entity Inspector");
            return true;
        }
    }
}