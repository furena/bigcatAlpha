using AzureB2BInvite.Utils;
using B2BPortal.Infrastructure;
using B2BPortal.Infrastructure.Filters;
using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace B2BPortal.Areas.Admin.Controllers
{
    [AuthorizedInviter]
    public class BulkInviteController : Controller
    {
        // GET: Admin/BulkInvite
        public async Task<ActionResult> Index()
        {
            var res = new List<SelectListItem>();
            var templates = (await TemplateUtilities.GetTemplates()).ToList();

            res.AddRange(templates.Select(t => new SelectListItem { Selected = (t.TemplateName == "Default"), Text = t.TemplateName, Value = t.Id }));
            ViewBag.Templates = res;
            return View();
        }

        public ActionResult Status()
        {
            return View();
        }
    }
}