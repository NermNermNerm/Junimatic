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
    private const string DoJumpMessageId = "Junimatic.DoJump";
    private const string StartBallMessageId = "Junimatic.StartBall";
    private const string RemoveBallMessageId = "Junimatic.RemoveBall";
    private const string CreateBalloonMessageId = "Junimatic.CreateBalloon";
    private const string DescendBalloonMessageId = "Junimatic.DescendBalloon";

    private GameBall? gameBall = null;
    private Balloon? balloon = null;
    private Child? balloonChild = null;

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

    record JumpData(string jumper, float jumpVelocity);

    public void BroadcastJump(Character jumper, float jumpVelocity = 8f)
    {
        this.Helper.Multiplayer.SendMessage( new JumpData(jumper.Name, jumpVelocity), PlaymateMultiplayerSupport.DoJumpMessageId, [this.Mod.ModManifest.UniqueID], null);
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

    private record CreateBalloonData(int floatHeightInTiles, string nameOfChildToPlayWith);

    public void BroadcastCreateBalloon(int floatHeightInTiles, Child childToPlayWith)
    {
        this.Helper.Multiplayer.SendMessage(new CreateBalloonData(floatHeightInTiles, childToPlayWith.Name), PlaymateMultiplayerSupport.CreateBalloonMessageId, [this.Mod.ModManifest.UniqueID]);
    }

    public void BroadcastDescendBalloon()
    {
        this.Helper.Multiplayer.SendMessage("", PlaymateMultiplayerSupport.DescendBalloonMessageId, [this.Mod.ModManifest.UniqueID]);
    }

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (Game1.player.currentLocation is not FarmHouse)
        {
            return;
        }

        switch (e.Type)
        {
            case PlaymateMultiplayerSupport.DoEmoteMessageId:
            {
                var d = e.ReadAs<EmoteData>();
                var c = Game1.getCharacterFromName(d.Emoter, mustBeVillager: false);
                c?.doEmote(d.EmoteId);
                break;
            }

            case PlaymateMultiplayerSupport.DoJumpMessageId:
            {
                var d = e.ReadAs<JumpData>();
                var c = Game1.getCharacterFromName(d.jumper, mustBeVillager: false);
                c?.jump(d.jumpVelocity);
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

            case PlaymateMultiplayerSupport.CreateBalloonMessageId:
            {
                var farmHouse = Game1.locations.First(l => l is FarmHouse);
                var d = e.ReadAs<CreateBalloonData>();
                this.balloonChild = Game1.getCharacterFromName(d.nameOfChildToPlayWith, mustBeVillager: false) as Child;
                if (this.balloonChild is null)
                {
                    this.LogError($"Could not find a child named '{d.nameOfChildToPlayWith}'");
                    break;
                }

                this.balloon = new Balloon(farmHouse, d.floatHeightInTiles, this.balloonChild.Position);
                farmHouse.instantiateCrittersList(); // <- only does something if the critters list is non-existent.
                farmHouse.addCritter(this.balloon); // <- if the critters list doesn't exist, this will do nothing.
                break;
            }

            case PlaymateMultiplayerSupport.DescendBalloonMessageId:
                if (this.balloon is not null)
                {
                    this.balloon.IsGoingDown = true;
                    var crawler = PlaymateMultiplayerSupport.GetPlaymate<JunimoCrawlerPlaymate>();
                    crawler.StartGrabbingJump(this.balloon);
                }
                break;

            default:
                break;
        };
    }

    private static T GetPlaymate<T>() where T : JunimoPlaymateBase
    {
        var farmHouse = Game1.locations.First(l => l is FarmHouse);
        return farmHouse.characters.OfType<T>().First();
    }
}
