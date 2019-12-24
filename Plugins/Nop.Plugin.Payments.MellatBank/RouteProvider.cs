using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.MellatBank
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Return
            routes.MapRoute("Plugin.Payments.MellatBank.Return",
                 "Plugins/PaymentMellatBank/Return",
                 new { controller = "PaymentMellatBank", action = "Return" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.MellatBank.PaymentCancelled",
                 "Plugins/PaymentMellatBank/PaymentCancelled",
                 new { controller = "PaymentMellatBank", action = "PaymentCancelled" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
