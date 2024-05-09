using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class GeneratedMail : ISimpleLog
    {
        private ModEntry mod = null!;

        private const string MailModDataPrefix = "JojaFinancial.Mail.";

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            this.mod.Helper.Events.Content.AssetRequested += this.Content_AssetRequested;
        }

        private void Content_AssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    this.AddMailKeys(data);
                });
            }
        }

        private void AddMailKeys(IDictionary<string, string> data)
        {
            foreach (var pair in Game1.player.modData.Pairs)
            {
                if (pair.Key.StartsWith(MailModDataPrefix))
                {
                    data[pair.Key.Substring(MailModDataPrefix.Length+1)] = pair.Value;
                }
            }
        }

        public virtual void SendMail(string idPrefix, string synopsis, string message, params (string qiid, int count)[] attachedItemQiids)
        {
            string mailKey = $"{idPrefix}.{Game1.Date.Year}.{Game1.Date.SeasonIndex}.{Game1.Date.DayOfMonth}";
            string value = message.Replace("\r", "").Replace("\n", "^");

            foreach (var pair in attachedItemQiids)
            {
                value += $"%item id {pair.qiid} {pair.count}%%";
            }
            value += "[#]" + synopsis;
            Game1.player.modData[$"{MailModDataPrefix}.{mailKey}"] = value;
            this.mod.Helper.GameContent.InvalidateCache("Data/Mail");
            Game1.player.mailForTomorrow.Add(mailKey);
        }

        public void SendMail(string idPrefix, string synopsis, string message, params string[] attachedItemQiids)
        {
            this.SendMail(idPrefix, synopsis, message, attachedItemQiids.Select(id => (id, 1)).ToArray());
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.mod.WriteToLog(message, level, isOnceOnly);
        }
    }
}
