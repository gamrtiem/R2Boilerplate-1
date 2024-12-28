using BepInEx;
using On.RoR2.Items;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterBody = On.RoR2.CharacterBody;
using Inventory = On.RoR2.Inventory;

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

        private static ItemDef boilingThermos;
        private static ItemDef boilingThermosUsed;
        public static BuffDef boilingThermosBuff;

        public int buffAmount = 8;

        public void Awake()
        {
            Log.Init(Logger);

            boilingThermos = ScriptableObject.CreateInstance<ItemDef>();
            boilingThermosUsed = ScriptableObject.CreateInstance<ItemDef>();

            boilingThermos.name = "SF_BOILINGTHERMOS_NAME";
            boilingThermos.nameToken = "SF_BOILINGTHERMOS_NAME";
            boilingThermos.pickupToken = "SF_BOILINGTHERMOS_PICKUP";
            boilingThermos.descriptionToken = "SF_BOILINGTHERMOS_DESC";
            boilingThermos.loreToken = "SF_BOILINGTHERMOS_LORE";
            
            boilingThermosUsed.name = "SF_BOILINGTHERMOSUSED_NAME";
            boilingThermosUsed.nameToken = "SF_BOILINGTHERMOSUSED_NAME";
            boilingThermosUsed.pickupToken = "SF_BOILINGTHERMOSUSED_PICKUP";
            boilingThermosUsed.descriptionToken = "SF_BOILINGTHERMOSUSED_DESC";
            boilingThermosUsed.loreToken = "SF_BOILINGTHERMOSUSED_LORE";
            
            boilingThermos._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset")
                .WaitForCompletion();
            boilingThermosUsed._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/NoTier.asset")
                .WaitForCompletion();

            boilingThermos.pickupIconSprite = Addressables
                .LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            boilingThermos.pickupModelPrefab = Addressables
                .LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    
            boilingThermosUsed.pickupIconSprite = Addressables
                .LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            
            boilingThermos.canRemove = true;
            boilingThermosUsed.canRemove = false;

            boilingThermos.hidden = false;
            boilingThermosUsed.hidden = false;
            boilingThermosUsed.tags = [ItemTag.WorldUnique]; // prevent usedthermos from being in item pool

            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(boilingThermos, displayRules));
            ItemAPI.Add(new CustomItem(boilingThermosUsed, displayRules));
            
            boilingThermosBuff = ScriptableObject.CreateInstance<BuffDef>();
            boilingThermosBuff.isDebuff = false;
            boilingThermosBuff.buffColor = Color.white;
            boilingThermosBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png")
                .WaitForCompletion();
            boilingThermosBuff.isCooldown = false;
            boilingThermosBuff.canStack = true;
            ContentAddition.AddBuffDef(boilingThermosBuff);
            
            Inventory.GiveItem_ItemIndex_int += Inventory_GiveItem_ItemDef_int;
            Inventory.RemoveItem_ItemIndex_int += Inventory_RemoveItem_ItemDef_int;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            MultiShopCardUtils.OnMoneyPurchase += MultiShopCardUtils_OnMoneyPurchase;
            CharacterBody.Start += CharacterBody_Start;
        }
        

        private void Inventory_RemoveItem_ItemDef_int(Inventory.orig_RemoveItem_ItemIndex_int orig, RoR2.Inventory self,
            ItemIndex itemindex, int count)
        {
            orig(self, itemindex, count);

            var thermosIndex = ItemCatalog.FindItemIndex(boilingThermos.name);
            if (itemindex == thermosIndex)
            {
                if (self.GetItemCount(boilingThermos) == 0)
                {
                    var buffCount = self.GetComponent<CharacterMaster>().GetBody().GetBuffCount(boilingThermosBuff);
                    for(int i = 0; i < buffCount; i++)
                    {
                        self.GetComponent<CharacterMaster>().GetBody().RemoveBuff(boilingThermosBuff);
                    }
                }

            }
            
        }

        private void Inventory_GiveItem_ItemDef_int(Inventory.orig_GiveItem_ItemIndex_int orig, RoR2.Inventory self, ItemIndex itemIndex, int count)
        {
            orig(self, itemIndex, count);
            if (self != null)
            {
                var thermosIndex = ItemCatalog.FindItemIndex(boilingThermos.name);
                if (itemIndex == thermosIndex && self.GetItemCount(thermosIndex) - count == 0) // only give buff on first pickup
                {
                    if (self.GetComponent<CharacterMaster>() != null)
                        for (int i = 0; i < buffAmount; i++)
                        {
                            self.GetComponent<CharacterMaster>().GetBody().AddBuff(boilingThermosBuff);
                        }
                } 
            }
        }

        private void CharacterBody_Start(CharacterBody.orig_Start orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (self.inventory != null)
            {
                int itemCount = self.inventory.GetItemCount(boilingThermosUsed);
                
                if (itemCount > 0)
                {
                    self.inventory.RemoveItem(boilingThermosUsed, itemCount);
                    self.inventory.GiveItem(boilingThermos, itemCount);
                }

                itemCount = self.inventory.GetItemCount(boilingThermos);

                if (itemCount > 0)
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(self.master, boilingThermosUsed.itemIndex, boilingThermos.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
            }
        }

        private void MultiShopCardUtils_OnMoneyPurchase(MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            orig(context);
            if (context.activatorBody.inventory.GetItemCount(boilingThermos) > 0)
            {
                context.activatorBody.RemoveBuff(boilingThermosBuff);
                if (context.activatorBody.GetBuffCount(boilingThermosBuff) % buffAmount == 0)
                {
                    for(int i = 0; i < context.activatorBody.inventory.GetItemCount(boilingThermos); i++)
                    {
                        context.activatorBody.inventory.GiveItem(boilingThermosUsed);
                    }
                    for (int i = 0; i < context.activatorBody.inventory.GetItemCount(boilingThermosUsed); i++) // we need to do it after because otherwise it changes how many loops it goes through and only removes like half lol
                    {
                        context.activatorBody.inventory.RemoveItem(boilingThermos);
                    }
                    CharacterMasterNotificationQueue.SendTransformNotification(context.activatorBody.master, boilingThermos.itemIndex, boilingThermosUsed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(RoR2.CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if(sender)
            {
                int count = sender.GetBuffCount(boilingThermosBuff);
                if(count != 0)
                {
                    Logger.LogInfo("starting moveSpeedMultAdd: " + args.moveSpeedMultAdd);
                    args.moveSpeedMultAdd += 0.035f * sender.GetBuffCount(boilingThermosBuff) + (4f - 0.035f * sender.GetBuffCount(boilingThermosBuff)) * (1f - 1f / (1f + 0.009f * sender.GetBuffCount(boilingThermosBuff) * (sender.inventory.GetItemCount(boilingThermos) - 1f))); //(1f-1f/(1f+0.035f*sender.GetBuffCount(boilingThermosBuff)*(sender.inventory.GetItemCount(boilingThermos)))); //0.035f*48 + (5f - 0.035f) * (1 - 1 / (1 + 0.035f * ((sender.inventory.GetItemCount(boilingThermos)- 1 * sender.GetBuffCount(boilingThermosBuff)) ))); //baseValue + (maxValue - baseValue) * (1 - 1 / (1 + additionalValue * (itemCount - 1)));
                    Logger.LogInfo("Ending moveSpeedMultAdd: " + args.moveSpeedMultAdd);
                }
            }
        }

        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(boilingThermos.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
