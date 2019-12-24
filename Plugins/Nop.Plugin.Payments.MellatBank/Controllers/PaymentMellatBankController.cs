using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.MellatBank.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Core.Plugins;

namespace Nop.Plugin.Payments.MellatBank.Controllers
{
    public class PaymentMellatBankController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPluginFinder _pluginFinder;


        public PaymentMellatBankController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger,
            IPluginFinder pluginFinder,
            IWebHelper webHelper,
            PaymentSettings paymentSettings, 
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._paymentSettings = paymentSettings;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._pluginFinder = pluginFinder;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var mellatBankPaymentSettings = _settingService.LoadSetting<MellatBankPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.TransactModeId = Convert.ToInt32(mellatBankPaymentSettings.TransactMode);
            model.TerminalId = mellatBankPaymentSettings.TerminalId;
            model.Username = mellatBankPaymentSettings.Username;
            model.Password = mellatBankPaymentSettings.Password;
            model.AdditionalFee = mellatBankPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = mellatBankPaymentSettings.AdditionalFeePercentage;
            model.TransactModeValues = mellatBankPaymentSettings.TransactMode.ToSelectList();

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.TransactMode, storeScope);
                model.Username_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.Username, storeScope);
                model.Password_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.Password, storeScope);
                model.TerminalId_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.TerminalId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(mellatBankPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("Nop.Plugin.Payments.MellatBank.Views.PaymentMellatBank.Configure", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var mellatBankPaymentSettings = _settingService.LoadSetting<MellatBankPaymentSettings>(storeScope);

            //save settings
            mellatBankPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            mellatBankPaymentSettings.Username = model.Username;
            mellatBankPaymentSettings.Password = model.Password;
            mellatBankPaymentSettings.TerminalId = model.TerminalId;
            mellatBankPaymentSettings.AdditionalFee = model.AdditionalFee;
            mellatBankPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.TransactModeId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.TransactMode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.TransactMode, storeScope);

            if (model.Username_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.Username, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.Username, storeScope);

            if (model.Password_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.Password, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.Password, storeScope);

            if (model.TerminalId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.TerminalId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.TerminalId, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(mellatBankPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(mellatBankPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            return Configure();
        }


        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("Nop.Plugin.Payments.MellatBank.Views.PaymentMellatBank.PaymentInfo");
        }

        [ValidateInput(false)]
        public ActionResult Return(FormCollection form)
        {
            string strRefNum = Request["RefId"];
            string strResNum = Request["saleOrderId"];
            string strTransactionStatus = Request["ResCode"];
            string strReference_number = Request["SaleReferenceId"];

            if (strRefNum == null || strResNum == null || strTransactionStatus == null || strReference_number == null)
            {
                return RedirectToRoute("Plugin.Payments.MellatBank.PaymentCancelled");
            }



            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.MellatBank") as MellatBankPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("Mellat Bank module cannot be loaded");

            double res = 0;
            if (Double.TryParse(strResNum, out res) && Double.TryParse(strTransactionStatus, out res) && Double.TryParse(strReference_number, out res))
            {
                if (processor.VerifyPayment(strReference_number, strResNum))
                {
                    // payment has been verified

                    Order order = _orderService.GetOrderById(Convert.ToInt32(strResNum));
                    if (order != null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Mellat Bank MBT:");
                        sb.AppendLine("Transaction Status: " + strTransactionStatus);
                        sb.AppendLine("Reference Number: " + strReference_number);

                        //order note
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = sb.ToString(),
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        order.AuthorizationTransactionCode = strRefNum;
                        _orderService.UpdateOrder(order);

                        //load settings for a chosen store scope
                        var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
                        var mellatBankPaymentSettings = _settingService.LoadSetting<MellatBankPaymentSettings>(storeScope);

                        //mark order as paid
                        if (_orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            _orderProcessingService.MarkOrderAsPaid(order);
                        }

                        //send sms
                        //var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Mobile.SmsService");
                        //if (pluginDescriptor != null)
                        //{
                        //    var plugin = pluginDescriptor.Instance() as Nop.Plugin.Misc.Sms.Sms;
                        //    if (plugin != null)
                        //    {
                        //        //send SMS
                        //        if (plugin.SendSmsToAdmin("پرداخت جدید انجام شد. مبلغ:"+ order.OrderTotal.ToString()))
                        //        {
                        //            order.OrderNotes.Add(new OrderNote()
                        //            {
                        //                Note = "\"Order placed\" SMS alert (to store owner) has been sent",
                        //                DisplayToCustomer = false,
                        //                CreatedOnUtc = DateTime.UtcNow
                        //            });
                        //            _orderService.UpdateOrder(order);
                        //        }
                        //        else
                        //        {
                        //            order.OrderNotes.Add(new OrderNote()
                        //            {
                        //                Note = "\"Order placed\" SMS alert (to store owner) has NOT been sent due to some error at sms plugin or sms server.",
                        //                DisplayToCustomer = false,
                        //                CreatedOnUtc = DateTime.UtcNow
                        //            });
                        //            _orderService.UpdateOrder(order);
                        //            _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "\"Order placed\" SMS alert (to store owner) has NOT been sent due to some error at sms plugin or sms server.");
                        //        }
                        //    }


                        //}
                        

                    }
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
                else
                {
                    Order order = _orderService.GetOrderById(Convert.ToInt32(strResNum));
                    if (order != null)
                    {
                        //order note
                        order.OrderNotes.Add(new OrderNote()
                        {
                            Note = "اشکالی در عملیات پرداخت پیدا شد. عملیات وریفای پرداخت موفق نبود. " + processor.GetErrorDescription(Convert.ToInt32(strTransactionStatus)),
                            DisplayToCustomer = true,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }
                    return RedirectToRoute("Plugin.Payments.MellatBank.PaymentCancelled");
                }
            }
            else
            {
                Order order = _orderService.GetOrderById(Convert.ToInt32(strResNum));
                if (order != null)
                {
                    //order note
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = "اشکالی در عملیات پرداخت پیدا شد. مقادیر بازگشتی را چک کنید. " + processor.GetErrorDescription(Convert.ToInt32(strTransactionStatus)) + "---" + "refnum=" + strRefNum + "---" + "resnum=" + strResNum + "---" + "strTransactionStatus=" + strTransactionStatus + "---" + "strReference_number=" + strReference_number,
                        DisplayToCustomer = true,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                return RedirectToRoute("Plugin.Payments.MellatBank.PaymentCancelled");
            }
        }


        [ValidateInput(false)]
        public ActionResult PaymentCancelled()
        {

            return View("Nop.Plugin.Payments.MellatBank.Views.PaymentMellatBank.PaymentCancelled"); // payment canceled.
        }

    }
}
