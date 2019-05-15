using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;
using B2BPortal.Data;
using AzureB2BInvite;
using AzureB2BInvite.Models;
using B2BPortal.Common.Utils;
using B2BPortal.Common.Enums;
using AzureB2BInvite.Rules;
using System.Web.Security;
using System.IO;
using Encryption;

namespace BatchInviteProcessor
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        static SiteConfig CurrSiteConfig { get; set; }

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var connStr = ConfigurationManager.AppSettings["StorageConnectionString"];
            var config = new JobHostConfiguration
            {
                DashboardConnectionString = connStr,
                StorageConnectionString = connStr
            };


            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            Setup();

            config.NameResolver = new QueueNameResolver();

            var host = new JobHost(config);

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
        private static void Setup()
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

                bool isConfig = false;
                var task = Task.Run(async () => {
                    isConfig = await SiteConfigRules.LoadCurrSiteConfig();
                });
                task.Wait();

                if (isConfig)
                {
                    //if new site, no config but invites are disabled until config is complete
                    Settings.CurrSiteConfig.SiteRedemptionSettings = Settings.CurrSiteConfig.SiteRedemptionSettings;
                    Settings.CurrSiteConfig.InvitingOrg = Settings.CurrSiteConfig.InvitingOrg;
                    MailSender.MailFrom = Settings.CurrSiteConfig.SiteRedemptionSettings.InviterResponseEmailAddr;
                }

                Settings.AppRootPath = Environment.CurrentDirectory;

                Settings.AADInstanceLocal = ConfigurationManager.AppSettings["ida:AADInstanceLocal"];
                Settings.AADInstanceMulti = ConfigurationManager.AppSettings["ida:AADInstanceMulti"];
                Settings.TenantID = ConfigurationManager.AppSettings["ida:TenantId"];
                Settings.Tenant = ConfigurationManager.AppSettings["ida:Tenant"];

                Settings.AppClientId_Admin = ConfigurationManager.AppSettings["ida:ClientId_Admin"];
                Settings.AppClientSecret_Admin = ConfigurationManager.AppSettings["ida:ClientSecret_Admin"];
                //reusing same client password for token cache encryption
                AESEncryption.Password = Settings.AppClientSecret_Admin;

                Settings.AppClientId_Preauth = ConfigurationManager.AppSettings["ida:ClientId_PreAuth"];

                Settings.GraphApiVersion = ConfigurationManager.AppSettings["GraphApiVersion"];

                Settings.InviterRoleNames = (ConfigurationManager.AppSettings["InviterRoleNames"] as string).Split(',');
                Settings.AssignedInviterRole = ConfigurationManager.AppSettings["AssignedInviterRole"];

                //these are the mail content templates
                Settings.DefaultSubjectTemplateName = ConfigurationManager.AppSettings["DefaultSubjectTemplateName"];
                Settings.DefaultBodyTemplateName = ConfigurationManager.AppSettings["DefaultBodyTemplateName"];

                //SMTP config
                MailSender.MailEnabled = (ConfigurationManager.AppSettings["MailEnabled"] == "1");
                //this is the mail formatting template - subject and body are injected into this
                MailSender.MailTemplate = MailSender.GetTemplateContents(ConfigurationManager.AppSettings["MailTemplateName"]);
                MailSender.MailServer = ConfigurationManager.AppSettings["MailServer"];
                MailSender.MailServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPort"]);
                MailSender.SMTPLogin = ConfigurationManager.AppSettings["SMTPLogin"];
                MailSender.SMTPPassword = ConfigurationManager.AppSettings["SMTPPassword"];

                StorageRepo.StorageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
                StorageRepo.QueueName = ConfigurationManager.AppSettings["QueueName"];
                Settings.UseSMTP = (!string.IsNullOrEmpty(MailSender.MailServer));

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
