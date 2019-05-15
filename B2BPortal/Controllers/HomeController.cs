using AzureB2BInvite;
using AzureB2BInvite.Rules;
using B2BPortal.Infrastructure;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace B2BPortal.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (User.Identity.IsInAnyRole(Settings.InviterRoleNames))
            {
                return RedirectToAction("Index", new { controller = "Home", action = "Index", area = "Admin" });
            }

            if (!Settings.SiteConfigReady)
            {
                return View("NoConfig");
            }

            ViewBag.Title = string.Format("Request guest access to the {0} org", Settings.CurrSiteConfig.InvitingOrg);
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "About";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact";
            return View();
        }

        public async Task<ActionResult> TOS(string configId = null)
        {
            if (configId != null)
            {
                var config = await SiteConfigRules.GetConfig(configId);
                ViewBag.TOSContent = config.TOSDocument;
            }
            else
            {
                ViewBag.TOSContent = Settings.CurrSiteConfig.TOSDocument;
            }
            return View();
        }

        public ActionResult Error()
        {
            ViewBag.ErrorMessage = Request.QueryString["message"];
            return View();
        }

        public ActionResult Claims()
        {
            return View();
        }
        public ActionResult Branding(int id)
        {
            switch (id)
            {
                case 1:
                    return new FileContentResult(Settings.Branding.Illustration.Image, "image/png");
                case 2:
                    return new FileContentResult(Settings.Branding.TileDarkLogo.Image, "image/png");
                case 3:
                    return new FileContentResult(Settings.Branding.TileLogo.Image, "image/png");
                default:
                    return new FileContentResult(Settings.Branding.BannerLogo.Image, "image/png");
            }
        }
    }
}