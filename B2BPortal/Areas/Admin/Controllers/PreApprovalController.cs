using AzureB2BInvite.Models;
using B2BPortal.Data;
using B2BPortal.Infrastructure.Filters;
using B2BPortal.Infrastructure;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using AzureB2BInvite;
using AzureB2BInvite.Utils;
using Microsoft.Graph;
using System.IdentityModel.Claims;
using B2BPortal.Common.Models;

namespace B2BPortal.Areas.Admin.Controllers
{
    [AuthorizedInviter]
    public class PreApprovalController : AsyncController
    {
        private List<InviteTemplate> _templates;

        public PreApprovalController()
        {
            var task = Task.Run(async () => {
                _templates = (await TemplateUtilities.GetTemplates()).ToList();
            });
            task.Wait();
        }
        private IEnumerable<SelectListItem> GetTemplates(string templateId)
        {
            var res = new List<SelectListItem>();
            res.AddRange(_templates.Select(t => new SelectListItem { Selected = ((templateId==null) ? (t.TemplateName == "Default") : (t.Id == templateId)), Text = t.TemplateName, Value = t.Id }));
            return res;
        }

        public async Task<ActionResult> Index()
        {
            IEnumerable<PreAuthDomain> domains = null;
            try
            {
                domains = await PreAuthDomain.GetDomains();
            }
            catch (Exception)
            {
                //see if this domain needs the group object updated
                domains = await PreAuthDomain.RefreshAllPreAuthGroupData();
            }
            return View(domains);
        }

        [HttpPost]
        public async Task<ActionResult> Create(PreAuthDomain domain)
        {
            ViewBag.Templates = GetTemplates(domain.InviteTemplateId);

            if (ModelState.IsValid)
            {
                try
                {
                    domain.AuthUser = User.Identity.GetEmail();
                    var doc = await PreAuthDomain.AddDomain(domain);
                    return RedirectToAction("Index");
                }
                catch
                {
                    return View("Edit", domain);
                }
            }

            return View("Edit", domain);
        }

        public async Task<ActionResult> Edit(string id)
        {
            PreAuthDomain domain;
            if (id == null)
            {
                ViewBag.Operation = "Create";
                domain = new PreAuthDomain();
                domain.DomainRedemptionSettings.InviteRedirectUrl = string.Format("https://myapps.microsoft.com/{0}", Settings.Tenant);
                domain.DomainRedemptionSettings.InviterResponseEmailAddr = User.Identity.GetClaim(ClaimTypes.Email);
            }
            else
            {
                ViewBag.Operation = "Edit";
                domain = await PreAuthDomain.GetDomain(id);
            }

            ViewBag.Templates = GetTemplates(domain.InviteTemplateId);
            return View(domain);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(PreAuthDomain domain)
        {
            ViewBag.Templates = GetTemplates(domain.InviteTemplateId);

            if (ModelState.IsValid)
            {
                try
                {
                    domain.AuthUser = User.Identity.GetEmail();

                    if (domain.Id == null)
                    {
                        domain = await PreAuthDomain.AddDomain(domain);
                    }
                    else
                    {
                        domain = await PreAuthDomain.UpdateDomain(domain);
                    }

                    return RedirectToAction("Index");
                }
                catch
                {
                    ViewBag.Operation = "Edit";
                    return View(domain);
                }
            }
            return View(domain);
        }

        [HttpPost]
        public async Task<ActionResult> Delete(PreAuthDomain domain)
        {
            try
            {
                await PreAuthDomain.DeleteDomain(domain);
                return RedirectToAction("Index");
            }
            catch
            {
                ViewBag.Operation = "Delete";
                return RedirectToAction("Index");
            }
        }
    }
}