using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace NermNermNerm.Junimatic
{
    internal static class Patcher 
    {
        private static ModEntry Mod = null!;
        internal static void Apply(ModEntry mod, Harmony harmony)
        {
            Mod = mod;
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Farmer_gainExperience_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Farmer_addItemToInventoryBool_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.createItemDebris)),
                prefix: new HarmonyMethod(typeof(Patcher), nameof(Game1_createItemDebris_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "get_player"),
                postfix: new HarmonyMethod(typeof(Patcher), nameof(Game1_get_player_Postfix))
            );
        }

        // Track fake farmer
        internal static Farmer Harvester = null!;

        // Swap in fake farmer when Junimatic harvesting from garden pot
        internal static void Game1_get_player_Postfix(ref Farmer __result) 
        {
            // If the fake farmer tracking variable is null, this was not called by Junimatic
            if (Harvester is null) return;
            __result = Harvester;
        }

        // Suppress gainExperience from running if called by Junimatic
        internal static bool Farmer_gainExperience_Prefix()
        {
            // If the fake farmer tracking variable is not null, this was called by Junimatic
            if (Harvester is not null) return false; // Suppress experience gain
            return true;
        }

        // Intercept harvest item creation methods and add to IndoorPotMachine HarvestItems list
        internal static Action<SObject> Objects = null!;

        internal static bool Farmer_addItemToInventoryBool_Prefix(ref bool __result, Item item)
        {
            try
            {
                // If Objects tracking variable is not null, this was called by Junimatic
                if (Objects is not null)
                {
                    Objects((SObject)item);
                    __result = true; // Set the result returned to the code calling addItemToInventoryBool to true to indicate item successfully added
                    return false; // Return false to suppress addItemToInventoryBool from running
                }
                return true; // Return true to run addItemToInventoryBool as this was not called by Junimatic
            }
            catch (Exception ex)
            {
                Mod.LogError($"Failed in {nameof(Farmer_addItemToInventoryBool_Prefix)}:\n{ex}");
                return true;
            }
        }

        internal static bool Game1_createItemDebris_Prefix(ref Debris __result, Item item)
        {
            try
            {
                // If Objects tracking variable is not null, this was called by Junimatic
                if (Objects is not null)
                {
                    Objects((SObject)item);
                    __result = null!; // Set the result returned to the code calling createItemDebris to null as no Debris is created (returned result is not used)
                    return false; // Return false to suppress createItemDebris from running
                }
                return true; // Return true to run createItemDebris as this was not called by Junimatic
            }
            catch (Exception ex)
            {
                Mod.LogError($"Failed in {nameof(Game1_createItemDebris_Prefix)}:\n{ex}");
                return true;
            }
        }

        internal static bool Game1_createObjectDebris_Prefix(string id)
        {
            try
            {
                // If Objects tracking variable is not null, this was called by Junimatic
                if (Objects is not null)
                {
                    Objects(ItemRegistry.Create<SObject>(id)); // Create the object from the id passed to Game1.createObjectDebris
                    return false; // Return false to suppress createObjectDebris from running
                }
                return true; // Return true to run createObjectDebris as this was not called by Junimatic
            }
            catch (Exception ex)
            {
                Mod.LogError($"Failed in {nameof(Game1_createObjectDebris_Prefix)}:\n{ex}");
                return true;
            }
        }
    }
}