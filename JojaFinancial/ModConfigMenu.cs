using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class ModConfigMenu
    {
        private ModEntry mod = null!;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            mod.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            IManifest ModManifest = this.mod.ModManifest;
            ModConfig Config = ModEntry.Config;

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod configs
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.mod.Helper.WriteConfig(Config)
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Catalog Item Id's",
                getValue: () => Config.Catalogs,
                setValue: value => {
                    Config.Catalogs = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                }
            );
        }

    }
}
