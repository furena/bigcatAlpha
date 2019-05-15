using B2BPortal.Data;
using B2BPortal.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AzureB2BInvite;
using System.Security.Cryptography.X509Certificates;
using B2BPortal.Common.Utils;
using AzureB2BInvite.Rules;
using System.Threading.Tasks;
using B2BPortal.Common.Models;
using AzureB2BInvite.Models;
using System.IO;
using Encryption;

namespace B2BPortal
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                //DocDB config
                DocDBRepo.Settings.DocDBUri = ConfigurationManager.AppSettings["DocDBUri"];
                DocDBRepo.Settings.DocDBAuthKey = ConfigurationManager.AppSettings["DocDBAuthKey"];
                DocDBRepo.Settings.DocDBName = ConfigurationManager.AppSettings["DocDBName"];
                DocDBRepo.Settings.DocDBCollection = ConfigurationManager.AppSettings["DocDBCollection"];

                var client = DocDBRepo.Initialize().Result;
                var s = client.AuthKey;

                ControllerBuilder.Current.DefaultNamespaces.Add("B2BPortal.Controllers");
                AreaRegistration.RegisterAllAreas();
                GlobalConfiguration.Configure(WebApiConfig.Register);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                AntiForgeryConfig.UniqueClaimTypeIdentifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";

                //Settings
                bool isConfig = false;
                var task = Task.Run(async () => {
                    isConfig = await SiteConfigRules.LoadCurrSiteConfig();
                });
                task.Wait();

                if (isConfig)
                {
                    //if new site, no config but invites are disabled until config is complete
                    MailSender.MailFrom = Settings.CurrSiteConfig.SiteRedemptionSettings.InviterResponseEmailAddr;
                }

                Settings.AADInstanceLocal = ConfigurationManager.AppSettings["ida:AADInstanceLocal"];
                Settings.AADInstanceMulti = ConfigurationManager.AppSettings["ida:AADInstanceMulti"];
                Settings.TenantID = ConfigurationManager.AppSettings["ida:TenantId"];
                Settings.Tenant = ConfigurationManager.AppSettings["ida:Tenant"];
                Settings.AppRootPath = HttpRuntime.AppDomainAppPath;

                Settings.AppClientId_Admin = ConfigurationManager.AppSettings["ida:ClientId_Admin"];
                Settings.AppClientSecret_Admin = ConfigurationManager.AppSettings["ida:ClientSecret_Admin"];
                //using same client password for token cache encryption
                AESEncryption.Password = Settings.AppClientSecret_Admin;

                Settings.AppClientId_Preauth = ConfigurationManager.AppSettings["ida:ClientId_PreAuth"];

                Settings.GraphApiVersion = ConfigurationManager.AppSettings["GraphApiVersion"];

                Settings.DefaultSubjectTemplateName = ConfigurationManager.AppSettings["DefaultSubjectTemplateName"];
                Settings.DefaultBodyTemplateName = ConfigurationManager.AppSettings["DefaultBodyTemplateName"];
                Settings.InviterRoleNames = (ConfigurationManager.AppSettings["InviterRoleNames"] as string).Split(',');
                Settings.AssignedInviterRole = ConfigurationManager.AppSettings["AssignedInviterRole"];

                //SMTP config
                MailSender.MailEnabled = (ConfigurationManager.AppSettings["MailEnabled"] == "1");
                MailSender.MailTemplate = MailSender.GetTemplateContents(ConfigurationManager.AppSettings["MailTemplateName"]);
                MailSender.MailServer = ConfigurationManager.AppSettings["MailServer"];
                MailSender.MailServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPort"]);
                MailSender.SMTPLogin = ConfigurationManager.AppSettings["SMTPLogin"];
                MailSender.SMTPPassword = ConfigurationManager.AppSettings["SMTPPassword"];

                Settings.UseSMTP = (!string.IsNullOrEmpty(MailSender.MailServer));
                StorageRepo.StorageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
                StorageRepo.QueueName = ConfigurationManager.AppSettings["QueueName"];

                /*
                 * TODO: Prefetching the app token here because initializing this library during admin
                 * authentication is timing out/failing.
                 * don't know if this is due to the api call or spinning up this code
                 * see AdalUtil.CallGraph...
                */
                AdalUtil.AuthenticateApp();

                AdalUtil.GetTenantBranding();
                if (Settings.Branding.TileLogo.Image == null)
                {
                    //assign default logo
                    var imgPath = Path.Combine(Settings.AppRootPath, "Content\\Images\\AADB2BTile.png");
                    var img = File.ReadAllBytes(imgPath);
                    Settings.Branding.TileLogo.Image = img;
                }
                MailSender.Logo = Settings.Branding.TileLogo.Image;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("Error during site initialization", System.Diagnostics.EventLogEntryType.Error, ex);
                throw;
            }
        }
    }
}
