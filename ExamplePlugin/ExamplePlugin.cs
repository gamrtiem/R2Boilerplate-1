using System.Reflection;
using BepInEx;
using IL.EntityStates.BrotherMonster;
using On.RoR2.Items;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Bindings;
using CharacterBody = On.RoR2.CharacterBody;
using CharacterMaster = On.RoR2.CharacterMaster;
using Inventory = On.RoR2.Inventory;
using NetworkExtensions = On.RoR2.NetworkExtensions;

namespace ExamplePlugin
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class ExamplePlugin : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "soda got me on that sillyness!!";
        public const string PluginVersion = "1.0.0";

        // We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;
        private static ItemDef myItemDef2;
        public static BuffDef myBuffDef;
        public int itemStacks = 0;
        public int buffStacks = 0;
        public int buffAmount = 8;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            // First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef2 = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            myItemDef.name = "SF_BOILINGTHERMOS_NAME";
            myItemDef.nameToken = "SF_BOILINGTHERMOS_NAME";
            myItemDef.pickupToken = "SF_BOILINGTHERMOS_PICKUP";
            myItemDef.descriptionToken = "SF_BOILINGTHERMOS_DESC";
            myItemDef.loreToken = "SF_BOILINGTHERMOS_LORE";
            
            myItemDef2.name = "SF_BOILINGTHERMOSUSED_NAME";
            myItemDef2.nameToken = "SF_BOILINGTHERMOSUSED_NAME";
            myItemDef2.pickupToken = "SF_BOILINGTHERMOSUSED_PICKUP";
            myItemDef2.descriptionToken = "SF_BOILINGTHERMOSUSED_DESC";
            myItemDef2.loreToken = "SF_BOILINGTHERMOSUSED_LORE";

            // The tier determines what rarity the item is:
            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            // and finally NoTier is generally used for helper items, like the tonic affliction
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset")
                .WaitForCompletion();
            myItemDef2._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/NoTier.asset")
                .WaitForCompletion();
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // myItemDef.deprecatedTier = ItemTier.Tier2;

            // You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Addressables
                .LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables
                .LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    
            myItemDef2.pickupIconSprite = Addressables
                .LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            
            myItemDef.canRemove = true;
            myItemDef2.canRemove = false;

            myItemDef.hidden = false;
            myItemDef2.hidden = false;
            myItemDef2.tags = [ItemTag.WorldUnique];

            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ItemAPI.Add(new CustomItem(myItemDef2, displayRules));
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            myBuffDef.isDebuff = false;
            myBuffDef.buffColor = Color.white;
            myBuffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png")
                .WaitForCompletion();
            myBuffDef.isCooldown = false;
            myBuffDef.canStack = true;
            R2API.ContentAddition.AddBuffDef(myBuffDef);

            // But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.

            //CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            On.RoR2.Inventory.GiveItem_ItemIndex_int += Inventory_GiveItem_ItemDef_int;
            On.RoR2.Inventory.RemoveItem_ItemIndex_int += Inventory_RemoveItem_ItemDef_int;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            MultiShopCardUtils.OnMoneyPurchase += MultiShopCardUtils_OnMoneyPurchase;
            On.RoR2.CharacterBody.Start += CharacterBody_Start;
            
        }
        



        private void Inventory_RemoveItem_ItemDef_int(Inventory.orig_RemoveItem_ItemIndex_int orig, RoR2.Inventory self,
            ItemIndex itemindex, int count)
        {
            orig(self, itemindex, count);
            Logger.LogInfo("GiveItem_ItemDef_int " + itemindex);
            var itemindex2 = ItemCatalog.FindItemIndex(myItemDef.name);
            if (itemindex == itemindex2)
            {
                itemStacks = self.GetItemCount(myItemDef); // update itemstacks
                for(int j = 0; j < count; j++)
                {
                    if (buffStacks >= buffAmount)
                    {
                        buffStacks -= buffAmount; // since you can only pick up 1 item at a time, we only add 8
                        for(int i = 0; i < buffAmount; i++)
                        {
                            self.GetComponent<RoR2.CharacterMaster>().GetBody().RemoveBuff(myBuffDef);
                        }
                    }
                    else
                    {
                        for(int i = 0; i < buffStacks; i++)
                        {
                            self.GetComponent<RoR2.CharacterMaster>().GetBody().RemoveBuff(myBuffDef);
                        }
                        buffStacks = 0;

                    }
                }

            }
            
        }

        private void Inventory_GiveItem_ItemDef_int(Inventory.orig_GiveItem_ItemIndex_int orig, RoR2.Inventory self, ItemIndex itemIndex, int count)
        {
            orig(self, itemIndex, count);
            if (self != null)
            {
                var itemindex = ItemCatalog.FindItemIndex(myItemDef.name);
                if (itemIndex == itemindex)
                {
                    itemStacks = self.GetItemCount(myItemDef); // update itemstacks
                    for (int j = 0; j < count; j++)
                    {
                        buffStacks += buffAmount; // since you can only pick up 1 item at a time, we only add 8
                        if (self.GetComponent<RoR2.CharacterMaster>() != null)
                            for (int i = 0; i < buffAmount; i++)
                            {
                                self.GetComponent<RoR2.CharacterMaster>().GetBody().AddBuff(myBuffDef);
                            }
                    }
                }
            }
        }

        private void CharacterBody_Start(CharacterBody.orig_Start orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (self.inventory != null)
            {
                int itemCount = self.inventory.GetItemCount(myItemDef2);

                if (itemCount > 0)
                {
                    self.inventory.RemoveItem(myItemDef2, itemCount);
                    self.inventory.GiveItem(myItemDef, itemCount);
                    for(int i = 0; i < buffStacks; i++) // since we're adding the item again it actually addsthe buff twice and no good .,,.
                    {
                        self.RemoveBuff(myBuffDef);
                    }
                }

                itemCount = self.inventory.GetItemCount(myItemDef);

                if (itemCount > 0)
                {
                    itemStacks = itemCount; // update itemstacks

                    buffStacks = itemStacks * buffAmount; // since you can only pick up 1 item at a time, we only add 8

                    for (int i = 0; i < buffStacks; i++)
                    {
                        self.GetBody().AddBuff(myBuffDef);
                    }
                }
            }
        }

        private void MultiShopCardUtils_OnMoneyPurchase(MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            orig(context);
            if (context.activatorBody.inventory.GetItemCount(myItemDef) > 0)
            {
                buffStacks -= 1;
                context.activatorBody.RemoveBuff(myBuffDef);
                if (buffStacks % buffAmount == 0)
                {
                    context.activatorBody.inventory.GiveItem(myItemDef2);
                    context.activatorBody.inventory.RemoveItem(myItemDef);
                    if(buffStacks != 0)
                        for(int i = 0; i < buffAmount; i++)
                        {
                            context.activatorBody.AddBuff(myBuffDef);
                        }
                }

            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(RoR2.CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            
            if(sender)
            {
                int count = sender.GetBuffCount(myBuffDef);
                if(count != 0)
                {
                    Debug.Log("starting moveSpeedMultAdd: " + args.moveSpeedMultAdd);
                    args.moveSpeedMultAdd += 0.035f * buffStacks;
                    Debug.Log("Ending moveSpeedMultAdd: " + args.moveSpeedMultAdd);
                }
            }
            //sender.moveSpeed = sender.baseMoveSpeed * (sender.GetBuffCount(myBuffDef));
        }


        
        

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
