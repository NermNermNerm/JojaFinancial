using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class VGame1
    {
        public virtual WorldDate Date => Game1.Date;

        public virtual int PlayerMoney
        {
            get => Game1.player.Money;
            set => Game1.player.Money = value;
        }

        public virtual Random Random { get; } = new Random();

        public virtual string PlayerName => Game1.player.Name;

        public virtual string? GetPlayerModData(string modDataKey)
        {
            Game1.player.modData.TryGetValue(modDataKey, out string? value);
            return value;
        }

        public virtual void SetPlayerModData(string modDataKey, string? value)
        {
            if (value is null)
            {
                Game1.player.modData.Remove(modDataKey);
            }
            else
            {
                Game1.player.modData[modDataKey] = value;
            }
        }

        public virtual StardewValley.Object CreateObject(string itemId, int quantity)
            => ItemRegistry.Create<StardewValley.Object>(itemId, quantity);

        public virtual int? GetPriceOfItem(string itemId)
        {
            return ItemRegistry.Create<StardewValley.Object>(itemId)?.Price;
        }
    }
}
