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
                name: I18n.ModConfig_BaseGameFurniture,
                getValue: () => Config.UseRobinsFurnitureCatalogue,
                setValue: value => Config.UseRobinsFurnitureCatalogue = value,
                tooltip: I18n.ModConfig_BaseGameFurnitureTip
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.ModConfig_BaseGameWallpaper,
                getValue: () => Config.UsePierresWallpaperCatalogue,
                setValue: value => Config.UsePierresWallpaperCatalogue = value,
                tooltip: I18n.ModConfig_BaseGameWallpaperTip
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.ModConfig_Joja,
                getValue: () => Config.UseJojaCatalogue,
                setValue: value => Config.UseJojaCatalogue = value,
                tooltip: I18n.ModConfig_JojaTip
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.ModConfig_Wizard,
                getValue: () => Config.UseWizardCatalogue,
                setValue: value => Config.UseWizardCatalogue = value,
                tooltip: I18n.ModConfig_WizardTip
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.ModConfig_Retro,
                getValue: () => Config.UseRetroCatalogue,
                setValue: value => Config.UseRetroCatalogue = value,
                tooltip: I18n.ModConfig_RetroTip
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog1 ?? "",
                setValue: value => {
                    Config.ModCatalog1 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog2 ?? "",
                setValue: value => {
                    Config.ModCatalog2 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog3 ?? "",
                setValue: value => {
                    Config.ModCatalog3 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog3 ?? "",
                setValue: value => {
                    Config.ModCatalog3 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog4 ?? "",
                setValue: value => {
                    Config.ModCatalog4 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog5 ?? "",
                setValue: value => {
                    Config.ModCatalog5 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: I18n.ModConfig_ModCatalog,
                getValue: () => Config.ModCatalog6 ?? "",
                setValue: value => {
                    Config.ModCatalog6 = value;
                    // Spew errors ASAP.
                    _ = this.mod.GetConfiguredCatalogs();
                },
                tooltip: I18n.ModConfig_ModCatalogTip
            );
        }
    }
}
