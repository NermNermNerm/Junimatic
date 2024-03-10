using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;

namespace NermNermNerm.Junimatic
{
    internal class JunimoPortalQuestController : BaseQuestController
    {
        public JunimoPortalQuestController(ModEntry modEntry) : base(modEntry) { }

        // public override string? HintTopicConversationKey => ""; // TODO: Jodi's comment about the hut - converstation topic seems okay, but what should trigger it?
        protected override string ModDataKey => ModDataKeys.JunimoPortalQuestStatus;

        protected override string InitialQuestState => "Started";

        protected override BaseQuest CreateQuest()
        {
            return new JunimoPortalQuest(this);
        }

        protected override void OnDayStartedQuestNotStarted()
        {
            //if (Game1.getFarm().GetWeather().IsRaining)
            //{
            //    return; // TODO: Comment out for testing
            //}
        }

        protected override void OnStateChanged()
        {
            if (this.OverallQuestState == OverallQuestState.NotStarted)
            {
                this.MonitorInventoryForItem(ObjectIds.OldJunimoPortal, this.PlayerGotOldJunimoPortal);
            }
            else
            {
                this.StopMonitoringInventoryFor(ObjectIds.JunimoPortal);
            }
        }

        private void PlayerGotOldJunimoPortal(Item item)
        {
            if (this.IsStarted)
            {
                this.LogWarning($"Player found a broken attachment, {item.ItemId}, when the quest was active?!");
                return;
            }

            Game1.player.holdUpItemThenMessage(item);

            this.CreateQuestNew();
        }

        public void TestPlacePortal()
        {

            var farm = Game1.getFarm();
            var existing = farm.objects.Values.FirstOrDefault(o => o.ItemId == ObjectIds.OldJunimoPortal);
            if (existing is not null)
            {
                this.LogInfoOnce($"{ObjectIds.OldJunimoPortal} is already placed at {existing.TileLocation.X},{existing.TileLocation.Y}");
                return;
            }

            var k = farm.Objects.Keys.First();

            List<Vector2> weedLocations = farm.objects.Pairs.Where(pair => pair.Value.ItemId == "784" /* weed*/).Select(pair => pair.Key).ToList();
            if (weedLocations.Count == 0)
            {
                this.LogWarning("No weeds on farm, can't place the old junimo portal");
                return;
            }

            var position = weedLocations[Game1.random.Next(weedLocations.Count)];
            var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.OldJunimoPortal);
            o.questItem.Value = true;
            o.Location = Game1.getFarm();
            o.TileLocation = position;
            this.LogInfoOnce($"{ObjectIds.OldJunimoPortal} placed at {position.X},{position.Y}");
            o.IsSpawnedObject = true;
            farm.objects[o.TileLocation] = o;
        }
    }
}
