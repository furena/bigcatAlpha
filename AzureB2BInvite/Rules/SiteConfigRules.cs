using AzureB2BInvite;
using AzureB2BInvite.Models;
using B2BPortal.Common.Utils;
using B2BPortal.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.Web.Utilities;

namespace AzureB2BInvite.Rules
{
    public static class SiteConfigRules
    {
        public static async Task<IEnumerable<SiteConfig>> GetAllConfigs()
        {
            return (await DocDBRepo.DB<SiteConfig>.GetItemsAsync()).OrderByDescending(c => c.ConfigDate);
        }

        public static async Task<SiteConfig> GetCurrConfig()
        {
            var items = await DocDBRepo.DB<SiteConfig>.GetItemsAsync();
            var res = items.OrderBy(c => c.ConfigDate).LastOrDefault();
            if (res != null && res.InviteTemplateId != null)
            {
                res.InviteTemplateContent = (await DocDBRepo.DB<InviteTemplate>.GetItemAsync(res.InviteTemplateId));
            }
            return res;
        }

        /// <summary>
        /// Load latest site configuration record from the database.
        /// </summary>
        /// <returns>false if no record found, true indicates the latest record is available in Settings.CurrSiteConfig</returns>
        public static async Task<bool> LoadCurrSiteConfig()
        {
            try
            {
                Settings.CurrSiteConfig = await GetCurrConfig();

                if (Settings.CurrSiteConfig != null)
                {
                    if (Settings.CurrSiteConfig.InviteTemplateId != null)
                    {
                        Settings.CurrSiteConfig.InviteTemplateContent = await InviteTemplate.GetTemplate(Settings.CurrSiteConfig.InviteTemplateId);
                    }
                }

                Settings.SiteConfigReady = (Settings.CurrSiteConfig != null);
                return Settings.SiteConfigReady;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("Problem loading site config", System.Diagnostics.EventLogEntryType.Error, ex);
                Settings.SiteConfigReady = false;
                return Settings.SiteConfigReady;
            }
        }

        public static async Task<SiteConfig> GetConfig(string id)
        {
            var res = (await DocDBRepo.DB<SiteConfig>.GetItemAsync(id));
            if (res.InviteTemplateId != null)
            {
                res.InviteTemplateContent = (await DocDBRepo.DB<InviteTemplate>.GetItemAsync(res.InviteTemplateId));
            }
            return res;
        }

        public static async Task<SiteConfig> SetNewConfig(SiteConfig config)
        {
            config.ConfigDate = DateTime.UtcNow;
            config.ConfigVersion++;

            //TOSDocument is decorated with [AllowHtml], so clearing out dangerous tags
            if (!string.IsNullOrEmpty(config.TOSDocument))
            {
                config.TOSDocument = HtmlSanitizer.SanitizeHtml(config.TOSDocument);
            }

            config = (await DocDBRepo.DB<SiteConfig>.CreateItemAsync(config));

            return config;
        }
    }
}
