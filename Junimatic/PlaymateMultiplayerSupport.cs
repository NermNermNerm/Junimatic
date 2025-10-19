using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

namespace NermNermNerm.Junimatic;

public class PlaymateMultiplayerSupport : ModLet
{
    private const string DoEmoteMessageId = "Junimatic.DoEmote";
    private const string StartBallMessageId = "Junimatic.StartBall";
    private const string RemoveBallMessageId = "Junimatic.RemoveBall";
    private const string ArmFlapMessageId = "Junimatic.ArmFlap";

    private GameBall? gameBall = null;

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        this.Helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
    }

    record EmoteData(string Emoter, int EmoteId);

    public void BroadcastEmote(Character emoter, int emoteId)
    {
        this.Helper.Multiplayer.SendMessage( new EmoteData(emoter.Name, emoteId), PlaymateMultiplayerSupport.DoEmoteMessageId, [this.Mod.ModManifest.UniqueID], null);
    }

    private record BallData(Point StartingTile, Point EndingTile);

    public void BroadcastBall(Point startingTile, Point endingTile)
    {
        this.Helper.Multiplayer.SendMessage( new BallData(startingTile, endingTile), PlaymateMultiplayerSupport.StartBallMessageId, [this.Mod.ModManifest.UniqueID], null);
    }

    public void BroadcastRemoveBall()
    {
        this.Helper.Multiplayer.SendMessage("", PlaymateMultiplayerSupport.RemoveBallMessageId, [this.Mod.ModManifest.UniqueID], null);
    }

    public void BroadcastArmFlap(Character emoter)
    {
        this.Helper.Multiplayer.SendMessage(emoter.Name, PlaymateMultiplayerSupport.ArmFlapMessageId, [this.Mod.ModManifest.UniqueID], null);
    }

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (Game1.player.currentLocation is not FarmHouse)
        {
            return;
        }

        switch (e.Type)
        {
            case DoEmoteMessageId:
            {
                var d = e.ReadAs<EmoteData>();
                var c = Game1.getCharacterFromName(d.Emoter, mustBeVillager: false);
                c?.doEmote(d.EmoteId);
                break;
            }

            case PlaymateMultiplayerSupport.StartBallMessageId:
            {
                var d = e.ReadAs<BallData>();
                var farmHouse = Game1.locations.First(l => l is FarmHouse);
                this.gameBall = new GameBall(farmHouse, d.StartingTile, d.EndingTile, () => { });
                farmHouse.instantiateCrittersList(); // <- only does something if the critters list is non-existent.
                farmHouse.addCritter(this.gameBall); // <- if the critters list doesn't exist, this will do nothing.
                break;
            }

            case PlaymateMultiplayerSupport.RemoveBallMessageId:
                if (this.gameBall is not null)
                {
                    var farmHouse = Game1.locations.First(l => l is FarmHouse);
                    farmHouse.critters?.Remove(this.gameBall);
                    this.gameBall = null;
                }
                break;

            case PlaymateMultiplayerSupport.ArmFlapMessageId:
            {
                if (Game1.getCharacterFromName(e.ReadAs<string>(), mustBeVillager: false) is Child c)
                {
                    JunimoToddlerPlaymate.DoToddlerArmFlapAnimation(c);
                }
                break;
            }

            default:
                break;
        };
    }
}
