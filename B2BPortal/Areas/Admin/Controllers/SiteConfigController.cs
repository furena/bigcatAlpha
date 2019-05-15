using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using B2BPortal.Infrastructure;
using B2BPortal.Infrastructure.Filters;
using AzureB2BInvite.Models;
using AzureB2BInvite.Utils;
using B2BPortal.Data;
using AzureB2BInvite;
using AzureB2BInvite.Rules;

namespace B2BPortal.Areas.Admin.Controllers
{
    [AuthorizedInviter]
    public class SiteConfigController : Controller
    {
        private List<InviteTemplate> _templates;

        public SiteConfigController()
        {
            var task = Task.Run(async () => {
                _templates = (await TemplateUtilities.GetTemplates()).ToList();
            });
            task.Wait();
        }

        private IEnumerable<SelectListItem> GetTemplates(string templateId)
        {
            var res = new List<SelectListItem>();
            res.AddRange(_templates.Select(t => new SelectListItem { Selected = (t.Id == templateId), Text = t.TemplateName, Value = t.Id }));
            return res;
        }

        public async Task<ActionResult> Index()
        {
            var config = await SiteConfigRules.GetCurrConfig();
            if (config == null)
            {
                _templates = (await TemplateUtilities.InitializeDefaultTemplate(User.Identity.GetEmail())).ToList();
                var template = _templates.First();

                config = new SiteConfig
                {
                    InviteTemplateContent = template,
                    InviteTemplateId = template.Id
                };
            }

            ViewBag.Templates = GetTemplates(config.InviteTemplateId);

            return View("Edit", config);
        }

        public async Task<ActionResult> List()
        {
            var configs = await SiteConfigRules.GetAllConfigs();
            return View(configs);
        }

        public async Task<ActionResult> Details(string id)
        {
            var config = await SiteConfigRules.GetConfig(id);
            ViewBag.Templates = GetTemplates(config.InviteTemplateId);
            return View(config);
        }

        public async Task<ActionResult> Edit(string id)
        {
            ViewBag.Operation = "Edit";
            var config = await SiteConfigRules.GetConfig(id);
            ViewBag.Templates = GetTemplates(config.InviteTemplateId);

            return View(config);
        }

        public async Task<ActionResult> HistoryDetail(string id)
        {
            var config = await SiteConfigRules.GetConfig(id);
            ViewBag.Templates = GetTemplates(config.InviteTemplateId);

            return View(config);
        }

        [HttpPost]
        public async Task<ActionResult> Save(SiteConfig config)
        {
            ViewBag.Templates = GetTemplates(config.InviteTemplateId);

            if (ModelState.IsValid)
            {
                try
                {
                    config.ConfigAuthor = User.Identity.GetEmail();
                    config = await SiteConfigRules.SetNewConfig(config);
                    Settings.SiteConfigReady = true;
                    Settings.CurrSiteConfig = config;

                    //refresh invitation settings
                    MailSender.MailFrom = Settings.CurrSiteConfig.SiteRedemptionSettings.InviterResponseEmailAddr;

                    return RedirectToAction("Index");
                }
                catch
                {
                    ViewBag.Error = "An error occured saving your config, please check the error logs and try again.";
                    return View("Edit", config);
                }
            }
            return View("Edit", config);
        }
    }
}
