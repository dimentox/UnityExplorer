using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.IL2CPP;
using Unity.Entities;
using UnityExplorer;
using UnityExplorer.Inspectors;

namespace ECSExtension
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, VERSION)]
    public class ExtensionPlugin : BasePlugin
    {
        public const string PLUGIN_NAME = "ECS Inspector Extension";

        public const string PLUGIN_GUID = "org.kremnev8.plugin.ecs-inspector-extension";

        public const string VERSION = "1.0.0";
        
        public override void Load()
        {
            InspectorManager.customInspectors.Add(EntityAdder);
            Log.LogInfo("Added Entity Inspector");
        }

        private Type EntityAdder(object o)
        {
            if (o is World)
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