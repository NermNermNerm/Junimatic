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
        private const string InterestingTilesModDataKey = "PetFindsThings.InterestingTiles";
        private const string PetSawItemConversationKey = "PetFindsThings.PetSightedAnObject";

        private record IdAndPoint(string Id, Point Point)
        {
            public override string ToString() => FormattableString.Invariant($"{this.Point.X},{this.Point.Y},{this.Id}");

            public static IdAndPoint? FromString(string serialized)
            {
                string[] splits = serialized.Split(",", 3);
                if (splits.Length == 3 && int.TryParse(splits[0], out int x) && int.TryParse(splits[1], out int y))
                {
                    return new IdAndPoint(splits[2], new Point(x, y));
                }
                else
                {
                    return null;
                }
            }
        }

        public PetFindsThings() { }

        /// <summary>
        ///   Adds a new entry to the table of stuff that the pet might find for the given location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="id">Uniquely identifies the thing that the thing that the pet is discovering so, when the object is no longer able to be found, it can be removed without knowing the position.</param>
        /// <param name="tileLocation">The centerpoint for the pet position.  The pet will be positioned within 2 tiles of this spot.</param>
        /// <remarks>
        ///   The business with the ID accounts for the idea that the mod is monitoring the inventory for the item and
        ///   this gets triggered when the item is picked up.  In actuality, after you pick up an item, the old position
        ///   of the thing is still there in the object, but that doesn't seem to me like it's exactly intended behavior,
        ///   and thus might change.  So the ID provides some measure of version safety.
        /// </remarks>
        public void AddObjectForPetToFind(GameLocation location, string id, Point tileLocation)
        {
            this.LogTrace($"AddObjectForPetToFind({location.Name}, {id}, {tileLocation})");
            var d = this.Read(location);
            d[id] = new IdAndPoint(id, tileLocation);
            this.Write(location, d);
        }

        /// <summary>
        ///   Call this when the object that the pet was pointing out is no longer important.
        /// </summary>
        public void ObjectForPetToFindHasBeenPickedUp(GameLocation location, string id)
        {
            this.LogTrace($"ObjectForPetToFindHasBeenPickedUp({location.Name}, {id})");
            var d = this.Read(location);
            d.Remove(id);
            this.Write(location, d);
        }

        private Dictionary<string,IdAndPoint> Read(GameLocation location)
        {
            var result = new Dictionary<string, IdAndPoint>();
            bool hasErrors = false;
            if (location.modData.TryGetValue(InterestingTilesModDataKey, out string? oldValue))
            {
                foreach (string line in oldValue.Split("\n"))
                {
                    int.TryParse("5", out int x);
                    var value = IdAndPoint.FromString(line);
                    if (value is null)
                    {
                        hasErrors = true;
                    }
                    else
                    {
                        hasErrors |= result.ContainsKey(value.Id);
                        result[value.Id] = value;
                    }
                }
            }

            if (hasErrors)
            {
                this.LogError($"PetFindsThings mod data value is corrupt: {oldValue}");
            }
            return result;
        }

        private void Write(GameLocation location, Dictionary<string,IdAndPoint> values)
        {
            if (values.Count > 0)
            {
                location.modData[InterestingTilesModDataKey]
                    = string.Join("\n", values.Values.Select(iandp => iandp.ToString()));
            }
            else
            {
                location.modData.Remove(InterestingTilesModDataKey);
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
            var interestingItems = this.Read(e.NewLocation);
            var petInScene = e.NewLocation.characters.OfType<Pet>().FirstOrDefault();
            if (!interestingItems.Any()
                || petInScene is null
                || Game1.hudMessages.Any() // <- can protect against duplicate mods trying to do the same thing
                || Game1.getOnlineFarmers().Any(f => f != e.Player && f.currentLocation == e.NewLocation)
                || Game1.random.Next(100) >= 5) // 5% for it to happen.  Perhaps make the chances configurable?
            {
                return;
            }

            Point find = Game1.random.Choose(interestingItems.Values.Select(iandp => iandp.Point).ToArray());
            bool isObscured(Vector2 tile) => e.NewLocation.isBehindTree(tile) || e.NewLocation.isBehindBush(tile); // << TODO: behind building

            var openTiles = new List<Vector2>();
            for (int deltaX = -2; deltaX < 3; ++deltaX)
            {
                for (int deltaY = -2; deltaY < 3; ++deltaY)
                {
                    var tile = new Vector2(find.X + deltaX, find.Y + deltaY);
                    if (e.NewLocation.CanItemBePlacedHere(tile) && e.NewLocation.getObjectAt((int)tile.X, (int)tile.Y) is null && !e.NewLocation.terrainFeatures.ContainsKey(tile))
                    {
                        openTiles.Add(tile);
                    }
                }
            }

            if (!openTiles.Any())
            {
                this.LogWarning($"Can't put pet at {find} because the area is too crowded.");
                return;
            }

            var nonObscuredTiles = openTiles.Where(t => !isObscured(t)).ToArray();
            Vector2 landingTile = nonObscuredTiles.Any() ? Game1.random.Choose(nonObscuredTiles) : Game1.random.Choose(openTiles.ToArray());
            petInScene.Position = landingTile*64;

            Game1.addHUDMessage(new HUDMessage($"I wonder what {petInScene.Name} has been up to...") { noIcon = true });
            Game1.player.activeDialogueEvents[PetSawItemConversationKey] = 30;
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message,level, isOnceOnly);
    }
}
