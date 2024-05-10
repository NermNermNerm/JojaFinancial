using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValleyMods.JojaFinancial;

namespace JojaFinancial.Tests
{
    public class StubGeneratedMail
        : GeneratedMail
    {
        public record MailItem(string IdPrefix, string Synopsis, string Message, WorldDate SentDate, (string qiid, int count)[] attachedItems);

        public List<MailItem> SentMail = new();

        public override void SendMail(string idPrefix, string synopsis, string message, params (string qiid, int count)[] attachedItems)
        {
            this.SentMail.Add(new MailItem(idPrefix, synopsis, message, this.Mod.Game1.Date, attachedItems));
        }
    }
}
