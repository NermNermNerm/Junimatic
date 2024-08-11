using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewValley;

namespace NermNermNerm.Junimatic
{
    internal static class Patcher
    {
        private static ModEntry Mod = null!;

        /// <summary>
        /// Apply the harmony patches for Junimatic
        /// </summary>
        /// <param name="mod">The ModEntry instance for the Junimatic module</param>
        /// <param name="harmony">The Harmony instance for the Junimatic module</param>
        internal static void Apply(ModEntry mod, Harmony harmony)
        {
            Mod = mod;
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Farmer_addItemToInventoryBool_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.createItemDebris)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Game1_createItemDebris_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.createObjectDebris), [typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(GameLocation)]),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Game1_createObjectDebris_Prefix))
            );
        }

        // Redirect item creation methods to add items to Junimatic HarvestItems list
        internal static List<Item>? Objects = null;

        /// <summary>
        /// Prefix to patch addItemToInventoryBool in Farmer.cs
        /// Suppress addItemToInventoryBool when called during Junimatic harvesting logic and
        /// run the item against the logic in Objects instead.
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="item"></param>
        /// <returns>>False if called during Junimatic harvesting logic. Default: True</returns>
        internal static bool Farmer_addItemToInventoryBool_Prefix(ref bool __result, Item item)
        {
            // If Objects tracking variable is not null, this was called by Junimatic
            if (Objects is not null)
           {
                // None of this should reasonably throw, but an exception from a prefix could crash the game
                try
                {
                    // Group like items into a single entry if possible
                    AddToItemList(Objects, item);

                    __result = true; // Set the result returned to the code calling addItemToInventoryBool to true to indicate item successfully added
                    return false; // Return false to suppress addItemToInventoryBool from running
                }
                catch (Exception ex)
                {
                    Mod.LogError($"Failed in {nameof(Farmer_addItemToInventoryBool_Prefix)}:\n{ex}");
                }
            }

            return true; // Return true to run addItemToInventoryBool normally
        }

        /// <summary>
        /// Prefix to patch createItemDebris in Game1.cs
        /// Suppress createItemDebris when called during Junimatic harvesting logic and
        /// run the item against the logic in Objects instead.
        /// </summary>
        /// <param name="__result"></param>
        /// <param name="item"></param>
        /// <returns>>False if called during Junimatic harvesting logic. Default: True</returns>
        internal static bool Game1_createItemDebris_Prefix(ref Debris __result, Item item)
        {
            try
            {
                // If Objects tracking variable is not null, this was called by Junimatic
                if (Objects is not null)
                {
                    AddToItemList(Objects, item);
                    __result = null!; // Set the result returned to the code calling createItemDebris to null as no Debris is created (returned result is not used)
                    return false; // Return false to suppress createItemDebris from running
                }
            }
            catch (Exception ex)
            {
                Mod.LogError($"Failed in {nameof(Game1_createItemDebris_Prefix)}:\n{ex}");
            }

            return true; // Let the method run as normal
        }

        /// <summary>
        /// Prefix to patch createObjectDebris in Game1.cs
        /// Suppress createObjectDebris when called during Junimatic harvesting logic.
        /// Convert the id to an item and run it against the logic in Objects instead.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>>False if called during Junimatic harvesting logic. Default: True</returns>
        internal static bool Game1_createObjectDebris_Prefix(string id)
        {
            try
            {
                // If Objects tracking variable is not null, this was called by Junimatic
                if (Objects is not null)
                {
                    AddToItemList(Objects, ItemRegistry.Create(id));
                    return false; // Return false to suppress createObjectDebris from running
                }
            }
            catch (Exception ex)
            {
                Mod.LogError($"Failed in {nameof(Game1_createObjectDebris_Prefix)}:\n{ex}");
            }

            return true; // Let the method run as normal
        }

        private static void AddToItemList(List<Item> list, Item item)
        {
            var extantItem = list.FirstOrDefault(x => x.ItemId == item.ItemId && x.Quality == item.Quality && x.Stack + item.Stack < 1000);
            if (extantItem is not null)
            {
                extantItem.Stack += item.Stack;
            }
            else // Otherwise add a new entry
            {
                list.Add(item);
            }
        }
    }
}
