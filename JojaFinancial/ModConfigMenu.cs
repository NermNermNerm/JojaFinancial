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

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Base game furniture",
                getValue: () => Config.UseRobinsFurnitureCatalogue,
                setValue: value => Config.UseRobinsFurnitureCatalogue = value,
                tooltip: () => "The catalog Robin sells (200000g)"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Base game wallpaper and floor",
                getValue: () => Config.UsePierresWallpaperCatalogue,
                setValue: value => Config.UsePierresWallpaperCatalogue = value,
                tooltip: () => "The catalog Pierre sells (20000g)"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Joja",
                getValue: () => Config.UseJojaCatalogue,
                setValue: value => Config.UseJojaCatalogue = value,
                tooltip: () => "The Joja Furniture catalog (25000g)"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Wizard",
                getValue: () => Config.UseWizardCatalogue,
                setValue: value => Config.UseWizardCatalogue = value,
                tooltip: () => "The Wizard furniture catalog sold by Krobus (150000g)"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Retro",
                getValue: () => Config.UseRetroCatalogue,
                setValue: value => Config.UseRetroCatalogue = value,
                tooltip: () => "Retro furniture sold by the traveling cart (110000g)"
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog1 ?? "",
                setValue: value => {
                    Config.ModCatalog1 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog2 ?? "",
                setValue: value => {
                    Config.ModCatalog2 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog3 ?? "",
                setValue: value => {
                    Config.ModCatalog3 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog3 ?? "",
                setValue: value => {
                    Config.ModCatalog3 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog4 ?? "",
                setValue: value => {
                    Config.ModCatalog4 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog5 ?? "",
                setValue: value => {
                    Config.ModCatalog5 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Mod Catalog Qualified Item Id",
                getValue: () => Config.ModCatalog6 ?? "",
                setValue: value => {
                    Config.ModCatalog6 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: () => "An 'ItemId' of a furniture catalog supplied by a mod"
            );
        }
    }
}
