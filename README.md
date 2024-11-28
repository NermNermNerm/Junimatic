# Junimatic

This is the source code for the Stardew Valley mod, Junimatic.  It's a mod for people who can't play
without [Pathoschild's Automate mod](https://github.com/Pathoschild/StardewMods/tree/develop/Automate),
but feel real dirty for it.  For more details on what it does and installation, see
the [Nexus Page](https://www.nexusmods.com/stardewvalley/mods/22672?tab=description).

## Contributing

If you'd like to help with the mod, please file an 'issue' here on github first so that we can share
ideas and ensure that it's something that we can agree fits the mission of the mod.  From there, create
a pull request as normal.
  
## Translating the mods

The mod can be translated like other Stardew Valley mods.  Look in the game's installation folder,
then look for `Mods\Junimatic\i18n\default.json`.  Copy that to a file with your language code
(e.g. `es.json` for Spanish) and replace the English string values with the translated strings.

Please don't be a slave to accuracy.  If a line doesn't make sense or sound funny to you, it's likely
that it won't make sense to anybody who would want to use your translations.  For example, there's a line,
that Linus says, "Except for Jeremy Clarkson, my pet Schnauzer."  Schnauzer is a breed of dog whose
name just sounds funny and Jeremy Clarkson is an infamous BBC TV personality.  It's very likely that
people who don't speak English will have no idea who that is and won't think a Schnauzer is a particularly
funny-sounding name.  Pick something that will be funny in the language that you're translating to.

## Compiling the mods

Installing stable releases from Nexus Mods is recommended for most users. If you really want to
compile the mod yourself, read on.

These mods use the [crossplatform build config](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
so they can be built on Linux, Mac, and Windows without changes. See [the build config documentation](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
for troubleshooting.

Build the project in [Visual Studio](https://www.visualstudio.com/vs/community/) or [MonoDevelop](https://www.monodevelop.com/) to
build it and deploy it to your 'mod' directory in your Stardew Valley installation.

Launching it under the debugger will start Stardew Valley and your mod will be picked up as in the game.

### Compiling a mod for release

To package a mod for release:

1. Switch to `Release` build configuration.
2. Recompile the mod per the previous section.
3. Upload the generated `bin/Release/<mod name>-<version>.zip` file from the project folder.
