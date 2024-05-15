using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class ModConfig
    {
        public bool UseRobinsFurnitureCatalogue { get; set; } = true;
        public bool UsePierresWallpaperCatalogue { get; set; } = true;
        public bool UseJojaCatalogue { get; set; } = false;
        public bool UseWizardCatalogue { get; set; } = false;
        public bool UseJunimoCatalogue { get; set;} = false;
        public bool UseRetroCatalogue { get; set; } = false;
        public string? ModCatalog1 { get; set; } = null;
        public string? ModCatalog2 { get; set; } = null;
        public string? ModCatalog3 { get; set; } = null;
        public string? ModCatalog4 { get; set; } = null;
        public string? ModCatalog5 { get; set; } = null;
        public string? ModCatalog6 { get; set; } = null;
    }
}
