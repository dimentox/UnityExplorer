using System;
using BepInEx;
using BepInEx.Unity.Mono;
using UnityExplorer;

#if CPP
using BepInEx.Unity.IL2CPP;
#endif

namespace ECSExtension
{
    [BepInPlugin(Extension.PLUGIN_GUID, Extension.PLUGIN_NAME, Extension.VERSION)]
    [BepInDependency(ExplorerCore.GUID)]
#if CPP
    public class ExtensionPlugin : BasePlugin
    {
        public override void Load()
        {
            Extension.Load(Log);
        }

        public override bool Unload()
        {
            return Extension.Unload();
        }
    }
#else
    public class ExtensionPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Extension.Load(Logger);
        }

        private void OnDestroy()
        {
            Extension.Unload();
        }
    }
#endif
}