using AzureB2BInvite.Models;
using AzureB2BInvite.Rules;
using B2BPortal.Infrastructure.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace B2BPortal.Areas.Admin.Controllers
{
    public class HistoryController : Controller
    {
        [AuthorizedInviter]
        // GET: Admin/History
        public ActionResult Index()
        {
            return View();
        }
    }
}