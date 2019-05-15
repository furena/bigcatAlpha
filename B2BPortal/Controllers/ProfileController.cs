using AzureB2BInvite;
using AzureB2BInvite.Models;
using AzureB2BInvite.Rules;
using System.Linq;
using B2BPortal.Infrastructure;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using Microsoft.Owin.Security.Cookies;
using B2BPortal.Common.Helpers;
using B2BPortal.Common.Utils;

namespace B2BPortal.Controllers
{
    public class ProfileController : Controller
    {
        [Authorize]
        // GET: Profile
        public async Task<ActionResult> Index()
        {
            if (User.Identity.GetClaim("aud") != Settings.AppClientId_Admin)
            {
                //the user is accessing the profile editor but they aren't using the correct application - they may 
                //have a cached token from the previous pre-auth call. Bouncing them out to re-auth.
                HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                return Redirect("/account/signin?redir=/profile");
            }

            var oid = User.Identity.GetClaim(CustomClaimTypes.ObjectIdentifier);
            var user = ProfileManager.GetUserProfile(oid);
            if (user == null)
            {
                ViewBag.ErrorMessage = string.Format("User profile not found - please contact your administrator. (oid: {0})", oid);
                return View("Error");
            }
            ViewBag.RedirectLink = await ProfileManager.GetRedirUrl(User.Identity.Name);
            ViewBag.Message = "Edit profile";
            return View("Index", user);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> SignUp(GuestRequest request)
        {
            var r = new RegexUtilities();
            if (!r.IsValidEmail(request.EmailAddress))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            //sanity check: not trusting the passed-in preauth setting
            request.PreAuthed = (User.Identity.GetClaim(CustomClaimTypes.AuthType) == AuthTypes.B2EMulti);

            var result = await GuestRequestRules.SignUpAsync(request, Utils.GetProfileUrl(Request.Url));

            return View(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Update(AADUserProfile user)
        {
            if (ModelState.IsValid)
            {
                var oid = User.Identity.GetClaim(CustomClaimTypes.ObjectIdentifier);
                var redirectLink = await ProfileManager.GetRedirUrl(User.Identity.Name);
                if (redirectLink == null) redirectLink = "/Profile";

                var orgUser = ProfileManager.GetUserProfile(oid);
                var data = AADUserProfile.GetDeltaChanges(orgUser, user);

                ProfileManager.UpdateProfile(data, oid);
                return Redirect(redirectLink);
            }

            ViewBag.Message = "Edit profile";

            return View("Index", user);
        }

        public ActionResult RemoteProfile()
        {
            return View();
        }
    }
}