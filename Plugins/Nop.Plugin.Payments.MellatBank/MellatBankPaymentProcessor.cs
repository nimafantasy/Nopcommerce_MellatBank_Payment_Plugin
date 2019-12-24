using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.MellatBank.ir.shaparak.bpm;
using Nop.Plugin.Payments.MellatBank.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Core.Data;
using System.Linq;
using Nop.Core.Infrastructure;
using Nop.Web.Framework;
using Nop.Services.Logging;

namespace Nop.Plugin.Payments.MellatBank
{
    public class MellatBankPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly MellatBankPaymentSettings _mellatBankPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IEncryptionService _encryptionService;
        private readonly HttpContextBase _httpContext;
        private readonly IRepository<Order> _orderRepository;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public MellatBankPaymentProcessor(MellatBankPaymentSettings mellatBankPaymentSettings,
            ISettingService settingService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            ILogger logger,
            CurrencySettings currencySettings, IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService, IEncryptionService encryptionService, HttpContextBase httpContext)
        {
            this._mellatBankPaymentSettings = mellatBankPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._encryptionService = encryptionService;
            this._httpContext = httpContext;
            
            this._logger = logger;

        }

        #endregion

        #region Utilities


        /// <summary>
        /// Gets Authorize.NET URL
        /// </summary>
        /// <returns></returns>
        private string GetMellatBankUrl()
        {
            return "https://bpm.shaparak.ir/pgwchannel/startpay.mellat";
        }

        private string GetMellatBankCallBackUrl()
        {
            return _webHelper.GetStoreLocation(false) +"Plugins/PaymentMellatBank/Return";
        }

        public bool VerifyPayment(string referenceNumber, string resNum)
        {
            if (VerifyTransaction(referenceNumber, resNum))
                return true;
            else if (InquiryTransaction(referenceNumber, resNum))
                return true;
            else
                return false;
        }

        private bool VerifyTransaction(string reference_number, string res_num)
        {
            ir.shaparak.bpm.PaymentGatewayImplService bmService = new PaymentGatewayImplService();
            string strStatCode = bmService.bpVerifyRequest(_mellatBankPaymentSettings.TerminalId, _mellatBankPaymentSettings.Username, _mellatBankPaymentSettings.Password, Convert.ToInt64(res_num), Convert.ToInt64(res_num), Convert.ToInt64(reference_number));
            if (strStatCode.Trim() == "0")
            {
                // transaction verified
                return true;
            }
            else
                return false;
        }

        private bool InquiryTransaction(string reference_number, string res_num)
        {
            ir.shaparak.bpm.PaymentGatewayImplService bmService = new PaymentGatewayImplService();
            string strStatCode = bmService.bpInquiryRequest(_mellatBankPaymentSettings.TerminalId, _mellatBankPaymentSettings.Username, _mellatBankPaymentSettings.Password, Convert.ToInt64(res_num), Convert.ToInt64(res_num), Convert.ToInt64(reference_number));
            if (strStatCode.Trim() == "0")
            {
                // transaction verified
                return true;
            }
            else
                return false;
        }

        public string GetErrorDescription(int code)
        {
            switch (code)
            {
                case 0:
                    return "عملیات متوقف";
                case 1:
                    return "اشکال در انجام متد سرویس بانک";
                case 11:
                    return "شماره كارت نامعتبر است";
                case 12:
                    return "موجودي كافي نيست";
                case 13:
                    return "رمز نادرست است";
                case 14:
                    return "تعداد دفعات وارد كردن رمز بيش از حد مجاز است";
                case 15:
                    return "كارت نامعتبر است";
                case 16:
                    return "دفعات برداشت وجه بيش از حد مجاز است";
                case 17:
                    return "كاربر از انجام تراكنش منصرف شده است";
                case 18:
                    return "تاريخ انقضاي كارت گذشته است";
                case 19:
                    return "مبلغ برداشت وجه بيش از حد مجاز است";
                case 111:
                    return "صادر كننده كارت نامعتبر است";
                case 112:
                    return "خطاي سوييچ صادر كننده كارت";
                case 113:
                    return "پاسخي از صادر كننده كارت دريافت نشد";
                case 114:
                    return "دارنده كارت مجاز به انجام اين تراكنش نيست";
                case 21:
                    return "پذيرنده نامعتبر است";
                case 23:
                    return "خطاي امنيتي رخ داده است";
                case 24:
                    return "اطلاعات كاربري پذيرنده نامعتبر است";
                case 25:
                    return "مبلغ نامعتبر است";
                case 31:
                    return "پاسخ نامعتبر است";
                case 32:
                    return "فرمت اطلاعات وارد شده صحيح نمي باشد";
                case 33:
                    return "حساب نامعتبر است";
                case 34:
                    return "خطاي سيستمي";
                case 35:
                    return "تاريخ نامعتبر است";
                case 41:
                    return "شماره درخواست تكراري است";
                case 42:
                    return "يافت نشد Sale تراکنش";
                case 43:
                    return "قبلا درخواست Verify داده شده است";
                case 44:
                    return "درخواست Verify یافت نشد";
                case 45:
                    return "تراکنش Settle شده است";
                case 46:
                    return "تراکنش Settle نشده است";
                case 47:
                    return "تراکنش Settle یافت نشد";
                case 48:
                    return "تراکنش reverse شده است";
                case 49:
                    return "تراکنش refund یافت نشد";
                case 412:
                    return "شناسه قبض نادرست است";
                case 413:
                    return "شناسه پرداخت نادرست است";
                case 414:
                    return "سازمان صادر کننده قبض نامعتبر است";
                case 415:
                    return "زمان جلسه کاری به پایان رسیده است";
                case 416:
                    return "خطا در ثبت اطلاعات";
                case 417:
                    return "شناسه پرداخت کننده نامعتبر است";
                case 418:
                    return "اشکال در تعریف اطلاعات مشتری";
                case 419:
                    return "تعداد دفعات ورود اطلاعات از حد مجاز گذشته است";
                case 421:
                    return "آی پی نامعتبر است";
                case 51:
                    return "تراکنش تکراری است";
                case 54:
                    return "تراکنش مرجع موجود نیست";
                case 55:
                    return "تراکنش نامعتبر است";
                case 61:
                    return "خطا در واریز";

                default:
                    return "مشکلی در تراکنش ایجاد شده است.";
            }
        }
        /// <summary>
        /// Gets Authorize.NET API version
        /// </summary>
        //private string GetApiVersion()
        //{
        //    return "3.1";
        //}

        // Populate merchant authentication (ARB Support)
        //private MerchantAuthenticationType PopulateMerchantAuthentication()
        //{
        //    var authentication = new MerchantAuthenticationType();
        //    authentication.name = _authorizeNetPaymentSettings.LoginId;
        //    authentication.transactionKey = _authorizeNetPaymentSettings.TransactionKey;
        //    return authentication;
        //}
        /// <summary>
        ///  Get errors (ARB Support)
        /// </summary>
        /// <param name="response"></param>
        //private static string GetErrors(ANetApiResponseType response)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine("The API request failed with the following errors:");
        //    for (int i = 0; i < response.messages.Length; i++)
        //    {
        //        sb.AppendLine("[" + response.messages[i].code + "] " + response.messages[i].text);
        //    }
        //    return sb.ToString();
        //}
        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            ir.shaparak.bpm.PaymentGatewayImplService bmService = new PaymentGatewayImplService();
            string strPhaseOneResult = "";
            string strRefNum = "";
            string strStatCode = "";
            
            // Get the ref num to get started
            //string strdata = _mellatBankPaymentSettings.TerminalId + "##" + _mellatBankPaymentSettings.Username + "##" + _mellatBankPaymentSettings.Password + "##" + Convert.ToInt64(postProcessPaymentRequest.Order.Id).ToString() + "##" + Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal).ToString() + "##" + DateTime.Now.ToString("yyyyMMdd") + "##" + DateTime.Now.ToString("HHmmss") + "##" + GetMellatBankCallBackUrl();
            strPhaseOneResult = bmService.bpPayRequest(_mellatBankPaymentSettings.TerminalId, _mellatBankPaymentSettings.Username, _mellatBankPaymentSettings.Password, Convert.ToInt64(postProcessPaymentRequest.Order.Id), Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal), DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss"), "", GetMellatBankCallBackUrl(), 0);
            //_httpContext.Response.Redirect("http://asjhdgasjhdg.com/" + strPhaseOneResult + "-" + _mellatBankPaymentSettings.TerminalId + "-" + _mellatBankPaymentSettings.Username + "-" + _mellatBankPaymentSettings.Password + "-" + Convert.ToInt64(postProcessPaymentRequest.Order.Id) + "-" + Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal) + "-" + GetMellatBankCallBackUrl());

            if (strPhaseOneResult.Length > 0 && strPhaseOneResult.IndexOf(',') > 0)
            {
                string[] parts = strPhaseOneResult.Split(',');
                if (parts.Length == 2)
                {

                    if (Convert.ToInt32(parts[0]) == 0)
                    {
                        // its ok
                        strRefNum = parts[1].ToString();
                        strStatCode = parts[0].ToString();
                    }

                    // after getting the number check for duplicate in db in case of fraud

                    var query = from or in _orderRepository.Table
                                where or.AuthorizationTransactionCode == strRefNum
                                select or;

                    if (query.Count() > 0)
                    {
                        // THIS REFNUM ALREADY EXISTS,   H A L T   O P E R A T I O N
                        postProcessPaymentRequest.Order.PaymentStatus = PaymentStatus.Pending;
                        return;
                    }
                    else
                    {
                        // NO PREVIOUS RECORD OF REFNUM, CLEAR TO PROCEED


                        var remotePostHelper = new RemotePost();
                        remotePostHelper.FormName = "form1";
                        remotePostHelper.Url = GetMellatBankUrl();
                        remotePostHelper.Add("RefId", strRefNum);
                        remotePostHelper.Post();
                    }

                }
                else
                {
                    postProcessPaymentRequest.Order.PaymentStatus = PaymentStatus.Pending;
                    return;
                }
            }
            else
            {
                // no usable response from server
                postProcessPaymentRequest.Order.PaymentStatus = PaymentStatus.Pending;
                
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, GetErrorDescription(Convert.ToInt32(strPhaseOneResult)),strPhaseOneResult);
                return;
            }


        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _mellatBankPaymentSettings.AdditionalFee, _mellatBankPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            return result;
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");
            
            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentMellatBank";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.MellatBank.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentMellatBank";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.MellatBank.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentMellatBankController);
        }

        public override void Install()
        {
            //settings
            var settings = new MellatBankPaymentSettings()
            {
                TransactMode = TransactMode.Normal,
                TerminalId = 0,
                Username = "",
                Password = "",
                AdditionalFee = 0,
                AdditionalFeePercentage = false
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.RedirectionTip", "برای تکمیل پرداخت به پرتال پرداخت بانک هدایت خواهید شد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Notes", "در صورت استفاده از این روش واحد پول اصلی فروشگاه باید ریال باشد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactMode", "نوع تراکنش");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactMode.Hint", "نوع تراکنش را تعیین کنید");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactModeValues", "انواع تراکنش ها");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactModeValues.Hint", "انتخاب کنید.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId", "شناسه ترمینال");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId.Hint", "شناسه ترمینال که توسط بانک به شما ابلاغ می گردد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Username", "شناسه کاربر");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Username.Hint", "شناسه کاربر که توسط بانک به شما ابلاغ می گردد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Password", "رمز عبور");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Password.Hint", "رمز عبور که توسط بانک به شما ابلاغ می گردد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee", "کارمزد");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee.Hint", "کارمزد این شیوه پرداخت.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage", "کارمزد (بدون تیک = مقدار ثابت، تیک خورده = درصدی)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage.Hint", "تبدیل کارمزد ثابت به کارمزد درصدی. در صورت تیک خوردن این گزینه عدد مندرج در کارمزد به عنوان درصد کارمزد محاسبه خواهد شد.");

            
            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<MellatBankPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Notes");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactMode");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactMode.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactModeValues");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TransactModeValues.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Username");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Username.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage.Hint");
            
            base.Uninstall();
        }

        #endregion

        #region Properies


        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.Manual;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        public string PaymentMethodDescription => throw new NotImplementedException();

        #endregion
    }
}
