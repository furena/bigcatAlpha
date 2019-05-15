using B2BPortal.Data;
using B2BPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using AzureB2BInvite.Models;
using System.Threading.Tasks;
using B2BPortal.Interfaces;
using Microsoft.Azure.Documents.Client;

namespace B2BPortal.Infrastructure
{
    public static class Settings
    {
        public static string AppRootPath = HttpContext.Current.Server.MapPath("//");
        public static bool SiteConfigReady {get; set;}
        public static SiteConfig CurrSiteConfig { get; set; }
        public static string StorageConnectionString { get; set; }

        /// <summary>
        /// If the SMTP "MailServer" configuration settings is null or empty in web.config, this will be set to false
        /// If false, mail template content will be injected as additional messages within the Microsoft
        /// B2B invite default template, and invitation messages will be sent by the Azure AD B2B process
        /// automatically.
        /// If SMTP settings are created, this will be true and custom templates will sent 
        /// independently of Azure
        /// </summary>
        public static bool UseSMTP { get; set; }

        public static IEnumerable<GraphRoleUser> AssignedInviteRoleUsers { get; set; }

        /// <summary>
        /// An array of paths that are allowed access by a multi-tenant authenticated visitor.
        /// These visitors are here for pre-authentication to the request page and to the "thank you" page
        /// after a request.
        /// </summary>
        public static string[] VisitorAllowedPaths = { @"/", @"/profile/signup", @"/account/signin" };

        public static string GetMailTemplate(string templateName)
        {
            var mailPath = Path.Combine(AppRootPath, @"Templates\" + templateName);
            return File.ReadAllText(mailPath);
        }
        /// <summary>
        /// Load latest site configuration record from the database.
        /// </summary>
        /// <returns>false if no record found, true indicates the latest record is available in Settings.CurrSiteConfig</returns>
        public static bool LoadCurrSiteConfig()
        {
            try
            {
                CurrSiteConfig = DocDBRepo.DB<SiteConfig>.GetItemsAsync(c => c.DocType == DocTypes.SiteConfig).Result.LastOrDefault();
                
                if (CurrSiteConfig != null)
                {
                    if (CurrSiteConfig.InviteTemplateId != null)
                    {
                        CurrSiteConfig.InviteTemplateContent = InviteTemplate.GetTemplate(CurrSiteConfig.InviteTemplateId).GetAwaiter().GetResult();
                    }
                }

                SiteConfigReady = (CurrSiteConfig != null);
                return SiteConfigReady;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("Problem loading site config", System.Diagnostics.EventLogEntryType.Error, ex);
                SiteConfigReady = false;
                return SiteConfigReady;
            }
        }

        /// <summary>
        /// Write a new SiteConfig record. The latest record is returned by LoadCurrSiteConfig, and older configs are stored 
        /// for history (terms of service are stored in these config versions)
        /// </summary>
        /// <param name="config"></param>
        public static async void UpdateCurrSiteConfig(SiteConfig config)
        {
            await DocDBRepo.DB<SiteConfig>.CreateItemAsync(config);
            CurrSiteConfig = config;
        }
    }
}