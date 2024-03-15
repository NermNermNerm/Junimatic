using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This represents all the game content related to enabling the Junimo that
    ///   works Kegs, Casks and JellyJamJar machines.
    /// </summary>
    public class CropMachineHelper
    {
        public CropMachineHelper() { }

        public const string GiantCropCelebrationEventId = "Junimatic.CropMachineHelper.GiantCropCelebration";
        public const string EventCustomConditionGiantCropIsGrowingOnFarm = "Junimatic.GiantCropIsGrowingOnFarm";
        public const string EventCustomCommandFocusOnGiantCrop = "Junimatic.FocusOnGiantCrop";
        public const string EventCustomCommandSpringJunimosFromCrop = "Junimatic.SpringJunimosFromCrop";
        public const string EventCustomCommandJunimosDisappear = "Junimatic.JunimosDisappear";

        public void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            Event.RegisterPrecondition(EventCustomConditionGiantCropIsGrowingOnFarm, this.GiantCropIsGrowingOnFarm);
            Event.RegisterCommand(EventCustomCommandFocusOnGiantCrop, this.FocusOnGiantCrop);
            Event.RegisterCommand(EventCustomCommandSpringJunimosFromCrop, this.SpringJunimosFromCrop);
            Event.RegisterCommand(EventCustomCommandJunimosDisappear, this.JunimosDisappear);
        }

        private bool GiantCropIsGrowingOnFarm(GameLocation location, string eventId, string[] args)
            => location.resourceClumps.OfType<GiantCrop>().Any();

        private void FocusOnGiantCrop(Event @event, string[] split, EventContext context)
        {
            try
            {
                var crop = context.Location.resourceClumps.OfType<GiantCrop>().First();

                int xTile = (int)crop.Tile.X;
                int yTile = (int)crop.Tile.Y;
                Game1.viewportFreeze = true;
                Game1.viewport.X = xTile * 64 + 32 - Game1.viewport.Width / 2;
                Game1.viewport.Y = yTile * 64 + 32 - Game1.viewport.Height / 2;
                if (Game1.viewport.X > 0 && Game1.viewport.Width > Game1.currentLocation.Map.DisplayWidth)
                {
                    Game1.viewport.X = (Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width) / 2;
                }

                if (Game1.viewport.Y > 0 && Game1.viewport.Height > Game1.currentLocation.Map.DisplayHeight)
                {
                    Game1.viewport.Y = (Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height) / 2;
                }
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }

        private void SpringJunimosFromCrop(Event @event, string[] split, EventContext context)
        {
            try
            {
                var crop = context.Location.resourceClumps.OfType<GiantCrop>().First();
                // Let's see how we get on if we just jump any old place regardless of crap in the way.
                Vector2[] vectors = new Vector2[] { new Vector2(-2, 0), new Vector2(-2, -2), new Vector2(0, -2), new Vector2(2,-1), new Vector2(2,0), new Vector2(2,2), new Vector2(0,2), new Vector2(-1,2)};
                for (int i = 0; i < vectors.Length; ++i)
                {
                    var junimo = new EventJunimo(crop.Tile+new Vector2(1,1), vectors[i]);
                    @event.actors.Add(junimo);
                }
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }

        private void JunimosDisappear(Event @event, string[] split, EventContext context)
        {
            try
            {
                foreach (var junimo in @event.actors.OfType<EventJunimo>())
                {
                    junimo.GoBack();
                }
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }

        public bool IsUnlocked() => Game1.MasterPlayer.eventsSeen.Contains(GiantCropCelebrationEventId);


        private void OnAssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farm"))
            {
                e.Edit(editor =>
                {
                    var d = editor.AsDictionary<string, string>().Data;
                    d[$"{GiantCropCelebrationEventId}/sawEvent {ObjectIds.JunimoPortalDiscoveryEvent}/{EventCustomConditionGiantCropIsGrowingOnFarm}"] = $@"playful/
-1000 -1000/
farmer 8 24 0/
skippable/
{EventCustomCommandFocusOnGiantCrop}/
pause 2000/
{EventCustomCommandSpringJunimosFromCrop}/
pause 2000/
spriteText 4 ""We love giant crops!  Please keep growing them!""/
spriteText 4 ""One of us will come and help with your kegs, casks and preserves jars if you connect them to a portal!""/
{EventCustomCommandJunimosDisappear}/
spriteText 4 ""Thx!  Bai!!!""/
pause 2000/
end
";
                });
            }
        }


    }
}
