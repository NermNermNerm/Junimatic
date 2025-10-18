using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoPlaymateBase : JunimoBase
    {
        public JunimoPlaymateBase(GameLocation currentLocation, Color color, AnimatedSprite sprite, Vector2 position, int facingDir, string name, LocalizedContentManager? content = null)
             : base(currentLocation, color, sprite, position, facingDir, name, content)
        {
        }

        public JunimoPlaymateBase()
        {
        }

        public void BroadcastEmote(int emoteId)
        {
            this.doEmote(emoteId);
            ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastEmote(this, emoteId);
        }

        public void BroadcastEmote(Character emoter, int emoteId)
        {
            emoter.doEmote(emoteId);
            ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastEmote(emoter, emoteId);
        }
    }
}
