using B2BPortal.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using AzureB2BInvite.Models;
using B2BPortal.Common.Enums;
using B2BPortal.Common.Utils;

namespace AzureB2BInvite
{
    public static class Settings
    {
        public static string GraphResource = "https://graph.microsoft.com";
        public static string ObjectIdentifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public static string AADInstanceLocal { get; set; }
        public static string TenantID { get; set; }
        public static string Tenant { get; set; }
        public static string AADInstanceMulti { get; set; }
        public static string GraphApiVersion { get; set; }
        public static string AppClientId_Admin { get; set; }
        public static string WebAppUrl { get; set; }
        public static string AppClientSecret_Admin { get; set; }
        public static string AppClientId_Preauth { get; set; }
        public static string DefaultSubjectTemplateName { get; set; }
        public static string DefaultBodyTemplateName { get; set; }
        public static string[] InviterRoleNames { get; set; }
        public static string AssignedInviterRole { get; set; }

        public static string AppRootPath = System.Web.Hosting.HostingEnvironment.MapPath("//");
        public static bool SiteConfigReady {get; set;}
        public static SiteConfig CurrSiteConfig { get; set; }

        public static TenantBranding Branding { get; set; }

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