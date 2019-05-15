using AzureB2BInvite;
using B2BPortal.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Westwind.Web.Utilities;

namespace AzureB2BInvite.Utils
{
    public static class TemplateUtilities
    {
        public static async Task<IEnumerable<InviteTemplate>> GetTemplates()
        {
            return (await DocDBRepo.DB<InviteTemplate>.GetItemsAsync()).OrderByDescending(c => c.LastUpdated);
        }

        public static async Task<InviteTemplate> GetTemplate(string id)
        {
            return (await DocDBRepo.DB<InviteTemplate>.GetItemAsync(id));
        }

        public static async Task<InviteTemplate> AddTemplate(InviteTemplate template)
        {
            template.LastUpdated = DateTime.UtcNow;
            return (await DocDBRepo.DB<InviteTemplate>.CreateItemAsync(template));
        }

        public static async Task<IEnumerable<InviteTemplate>> InitializeDefaultTemplate(string templateAuthor)
        {
            var template = new InviteTemplate
            {
                LastUpdated = DateTime.UtcNow,
                TemplateAuthor = templateAuthor,
                TemplateName = "Default",
                TemplateVersion = 1
            };
            template.SubjectTemplate = MailSender.GetTemplateContents(Settings.DefaultSubjectTemplateName);
            template.TemplateContent = MailSender.GetTemplateContents(Settings.DefaultBodyTemplateName);

            template = (await DocDBRepo.DB<InviteTemplate>.CreateItemAsync(template));
            var res = new List<InviteTemplate>
            {
                template
            };
            return res;
        }

        public static async Task<dynamic> DeleteTemplate(InviteTemplate template)
        {
            return (await DocDBRepo.DB<InviteTemplate>.DeleteItemAsync(template));
        }

        public static async Task<InviteTemplate> UpdateTemplate(InviteTemplate template)
        {
            template.LastUpdated = DateTime.UtcNow;
            template.TemplateVersion++;

            //TOSDocument is decorated with [AllowHtml], so clearing out dangerous tags
            template.TemplateContent = HtmlSanitizer.SanitizeHtml(template.TemplateContent);

            template = (await DocDBRepo.DB<InviteTemplate>.UpdateItemAsync(template));

            return template;
        }
    }
}