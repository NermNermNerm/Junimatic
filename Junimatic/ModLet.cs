using StardewModdingAPI;

namespace NermNermNerm.Junimatic
{
    public class ModLet : ISimpleLog
    {
        public virtual void Entry(ModEntry mod)
        {
            this.Mod = mod;
        }

        public ModEntry Mod { get; private set; } = null!;

        public IModHelper Helper => this.Mod.Helper;

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.Mod.WriteToLog(message, level, isOnceOnly);
        }
    }
}
