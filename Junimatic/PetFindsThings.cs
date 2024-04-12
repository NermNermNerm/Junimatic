using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   A modlet that makes it so the pet will sometimes hang out near quest-started items.
    /// </summary>
    public class PetFindsThings
        : ISimpleLog
    {
        private ModEntry mod = null!;

        // Using a distinct mod key, in the event this gets split out
        public const string HiddenObjectsModDataKey = "PetFindsThings.ObjectsThePetMightFind";
        public const string PetSawItemConversationKey = "PetFindsThings.PetSightedAnObject";

        public PetFindsThings() { }

        public static void AddObjectForPetToFind(GameLocation location, string qualifiedItemId)
        {
            if (location.modData.TryGetValue(HiddenObjectsModDataKey, out string? oldValue))
            {
                if (!oldValue.Split("\n").Contains(qualifiedItemId))
                {
                    location.modData[HiddenObjectsModDataKey] = oldValue + "\n" + qualifiedItemId;
                }
            }
            else
            {
                location.modData.Add(HiddenObjectsModDataKey, qualifiedItemId);
            }
        }

        public static void ObjectForPetToFindHasBeenFound(GameLocation location, string qualifiedItemId)
        {
            if (location.modData.TryGetValue(HiddenObjectsModDataKey, out string? oldValue))
            {
                var items = oldValue.Split("\n").ToList();
                int index = items.IndexOf(qualifiedItemId);
                items.Remove(qualifiedItemId);
                string newValue = string.Join("\n", items);
                if (newValue == "")
                {
                    location.modData.Remove(HiddenObjectsModDataKey);
                }
                else
                {
                    location.modData[HiddenObjectsModDataKey] = newValue;
                }
            }
        }

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.Player.Warped += this.Player_Warped;
            mod.Helper.Events.Content.AssetRequested += this.Content_AssetRequested;
        }

        private void Content_AssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Marnie"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data[PetSawItemConversationKey] = "Pets sometimes have an uncanny ability to spot missing things!$0#$b#Just last week I lost my favorite milking bucket.  I came across it a few days later and my cat, Muffin, was sleeping in it.$1#$b#Well, I guess she didn't exactly find it for me, but at least she knew where it was!$0";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Linus"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data["winter_Sun4"] = @"Do I miss my ""normal"" life on days like this?$2#$b#No, not really.  Except for Jeremy Clarkson...$1#$b#...My pet Schnauzer.  He had an uncanny ability to fetch the thing I wanted before I even knew I wanted it.$0";
                });
            }
        }

        private void Player_Warped(object? sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (!e.NewLocation.modData.TryGetValue(HiddenObjectsModDataKey, out string? thingsToLookFor))
            {
                return;
            }

            var petInScene = e.NewLocation.characters.OfType<Pet>().FirstOrDefault();
            if (petInScene is null)
            {
                return;
            }

            if (Game1.hudMessages.Any())
            {
                // If another mod copies this class, this block will prevent both finders from firing at once.
                // (However, it'll still double the chances of the thing firing.)
                return;
            }

            if (Game1.random.Next(100) < 5) // 5% for it to happen.  Perhaps make the chances configurable?
            {
                return;
            }

            var qualifiedItemIds = thingsToLookFor.Split("\n").ToHashSet();
            var possibleFinds = e.NewLocation.Objects.Values.Where(o => qualifiedItemIds.Contains(o.QualifiedItemId)).ToList();
            if (!possibleFinds.Any())
            {
                return;
            }
            var find = possibleFinds[Game1.random.Next(possibleFinds.Count)];
            bool isObscured(Vector2 tile) => e.NewLocation.isBehindTree(tile) || e.NewLocation.isBehindBush(tile); // << TODO: behind building

            var openTiles = new List<Vector2>();
            for (int deltaX = -2; deltaX < 3; ++deltaX)
            {
                for (int deltaY = -2; deltaY < 3; ++deltaY)
                {
                    var tile = new Vector2(find.TileLocation.X + deltaX, find.TileLocation.Y + deltaY);
                    if (e.NewLocation.CanItemBePlacedHere(tile) && e.NewLocation.getObjectAt((int)tile.X, (int)tile.Y) is null && !e.NewLocation.terrainFeatures.ContainsKey(tile))
                    {
                        openTiles.Add(tile);
                    }
                }
            }

            if (!openTiles.Any())
            {
                this.LogWarning($"Area around {find.QualifiedItemId} is too crowded to move the pet to it.");
                return;
            }

            var nonObscuredTiles = openTiles.Where(t => !isObscured(t)).ToArray();
            Vector2 landingTile = nonObscuredTiles.Any() ? Game1.random.Choose(nonObscuredTiles) : Game1.random.Choose(openTiles.ToArray());
            petInScene.Position = landingTile*64;

            Game1.addHUDMessage(new HUDMessage($"I wonder what {petInScene.Name} has been up to...") { noIcon = true });
            Game1.player.activeDialogueEvents.Add(PetSawItemConversationKey, 30);
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message,level, isOnceOnly);
    }
}
