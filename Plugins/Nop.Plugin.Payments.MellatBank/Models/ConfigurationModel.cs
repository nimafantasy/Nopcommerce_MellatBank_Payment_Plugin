using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.MellatBank.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        public int TransactModeId { get; set; }
        public bool TransactModeId_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.TransactMode")]
        public SelectList TransactModeValues { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.TerminalId")]
        public long TerminalId { get; set; }
        public bool TerminalId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.Username")]
        public string Username { get; set; }
        public bool Username_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.Password")]
        public string Password { get; set; }
        public bool Password_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }
    }
}
