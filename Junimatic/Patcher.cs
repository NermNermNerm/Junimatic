using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    internal static class Patcher
    {
        /// <summary>
        /// Apply the harmony patches for Junimatic
        /// </summary>
        internal static void Apply()
        {
            ModEntry.Instance.Harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Farmer_addItemToInventoryBool_Prefix))
            );
            ModEntry.Instance.Harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.createItemDebris)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Game1_createItemDebris_Prefix))
            );
            ModEntry.Instance.Harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.createObjectDebris), [typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(GameLocation)]),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Game1_createObjectDebris_Prefix))
            );
        }

        internal static List<Item> InterceptHarvest(Action harvestAction, GameLocation machineLocation)
        {
            var playerField = typeof(Game1).GetField(I("_player"), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var player = Game1.player;
            try
            {
                Patcher.collectedItems = new List<Item>();

                // Bushes (particularly Tea bushes) are broken without this hack.  Bush.inBloom (called during harvest)
                // relies on Game1.stats.DaysPlayed, which points to the farmer's DaysPlayed stat.  Without these
                // shenanigans, it'll always decide that the bush isn't actually ready to harvest.
                Patcher.fakePlayer.stats.DaysPlayed = player.stats.DaysPlayed;

                // Set the backing field for Game1.player so that calls to do animation on the farmer don't happen and
                //  farmer's experience isn't used to calculate the quality of the crops and no xp is gained.
                playerField!.SetValue(null, Patcher.fakePlayer);
                Patcher.fakePlayer.currentLocation = machineLocation;

                harvestAction();
                return Patcher.collectedItems;
            }
            finally
            {
                playerField!.SetValue(null, player);
                Patcher.collectedItems = null;
            }
        }


        private static List<Item>? collectedItems = null;
        private static Farmer fakePlayer = new FakeFarmer();

        private class FakeFarmer : Farmer
        {
            public FakeFarmer()
            {
                // MARGO throws errors if this isn't set (taken from Garden Pot - Automate source code)
                this.mostRecentlyGrabbedItem = new StardewValley.Object();
            }
            public override void gainExperience(int which, int howMuch) { }
        };

        /// <summary>
        /// Prefix patch to <see cref="Farmer.addItemToInventoryBool"/>.
        /// When called during Junimatic harvesting logic, collect the item into <see cref="collectedItems"/>
        /// instead of the usual implementation.
        /// </summary>
        private static bool Farmer_addItemToInventoryBool_Prefix(ref bool __result, Item item)
        {
            try
            {
                if (collectedItems is not null) // If inside InterceptHarvest
                {
                    // Group like items into a single entry if possible
                    AddToItemList(collectedItems, item);

                    __result = true; // Set the result returned to the code calling addItemToInventoryBool to true to indicate item successfully added
                    return false; // Return false to suppress addItemToInventoryBool from running
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.LogError($"Failed in {nameof(Farmer_addItemToInventoryBool_Prefix)}:\n{ex}");
            }

            return true; // Return true to run addItemToInventoryBool normally
        }

        /// <summary>
        /// Prefix patch to <see cref="Game1.createItemDebris"/>.
        /// When called during Junimatic harvesting logic, collect the item into <see cref="collectedItems"/>
        /// instead of the usual implementation.
        /// </summary>
        private static bool Game1_createItemDebris_Prefix(ref Debris __result, Item item)
        {
            try
            {
                if (collectedItems is not null) // If inside InterceptHarvest
                {
                    AddToItemList(collectedItems, item);
                    __result = null!; // Set the result returned to the code calling createItemDebris to null as no Debris is created (returned result is not used)
                    return false; // Return false to suppress createItemDebris from running
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.LogError($"Failed in {nameof(Game1_createItemDebris_Prefix)}:\n{ex}");
            }

            return true; // Let the method run as normal
        }

        /// <summary>
        /// Prefix patch to <see cref="Game1.createObjectDebris"/>.
        /// When called during Junimatic harvesting logic, collect the item into <see cref="collectedItems"/>
        /// instead of the usual implementation.
        /// </summary>
        private static bool Game1_createObjectDebris_Prefix(string id)
        {
            try
            {
                if (collectedItems is not null) // If inside InterceptHarvest
                {
                    AddToItemList(collectedItems, ItemRegistry.Create(id));
                    return false; // Return false to suppress createObjectDebris from running
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.LogError($"Failed in {nameof(Game1_createObjectDebris_Prefix)}:\n{ex}");
            }

            return true; // Let the method run as normal
        }

        /// <summary>
        ///   Adds <paramref name="item"/> to <paramref name="list"/> while merging stacks of common items.
        /// </summary>
        private static void AddToItemList(List<Item> list, Item item)
        {
            var extantItem = list.FirstOrDefault(x =>
                x.ItemId == item.ItemId
                && x.Quality == item.Quality
                && isColorMatch(x, item)
                && x.Stack + item.Stack < 1000);
            if (extantItem is not null)
            {
                extantItem.Stack += item.Stack;
            }
            else // Otherwise add a new entry
            {
                list.Add(item);
            }
        }

        private static bool isColorMatch(Item i1, Item i2)
        {
            if (i1 is ColoredObject c1)
            {
                return (i2 is ColoredObject c2) && c1.color.Value == c2.color.Value;
            }
            else
            {
                return i2 is not ColoredObject;
            }
        }
    }
}
