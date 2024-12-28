using BepInEx;
using R2API;

namespace ExamplePlugin
{
    [BepInDependency(ItemAPI.PluginGUID)]
    
    [BepInDependency(LanguageAPI.PluginGUID)]
    
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class ExamplePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "sodagotmeonthatsillyness";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);
        }
    }
}
