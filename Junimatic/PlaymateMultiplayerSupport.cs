using System.Linq;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace NermNermNerm.Junimatic;

public class PlaymateMultiplayerSupport : ModLet
{
    private const string DoEmoteMessageId = "Junimatic.DoEmote";

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        this.Helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
    }

    record EmoteData(string emoter, int emoteId);

    public void BroadcastEmote(Character emoter, int emoteId)
    {
        this.Helper.Multiplayer.SendMessage( new EmoteData(emoter.Name, emoteId), PlaymateMultiplayerSupport.DoEmoteMessageId, [this.Mod.ModManifest.UniqueID], null);
    }

    // TODO: Flap arms event

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (Game1.player.currentLocation is not FarmHouse)
        {
            return;
        }

        switch (e.Type)
        {
            case DoEmoteMessageId:
                var d = e.ReadAs<EmoteData>();
                var c = Game1.player.currentLocation.characters.FirstOrDefault(c => c.Name == d.emoter);
                c?.doEmote(d.emoteId);
                break;
            default:
                break;
        };
    }

}
