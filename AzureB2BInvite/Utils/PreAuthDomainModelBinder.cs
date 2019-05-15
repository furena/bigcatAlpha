using AzureB2BInvite.Models;
using B2BPortal.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace AzureB2BInvite.Utils
{
    public class PreAuthDomainModelBinder : DefaultModelBinder
    {
        /// <summary>
        /// Storing the group ID and group name in the domain object, and capturing both in the option value
        /// delimited by "=", which fails the model. Re-mapping here.
        /// </summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var model = base.BindModel(controllerContext, bindingContext) as PreAuthDomain;
            //clear only the Groups error so other errors can fall through
            var g = bindingContext.ModelState["Groups"];
            if (g != null)
                g.Errors.Clear();

            var res = new List<GroupObject>();
            var g2 = bindingContext.ValueProvider.GetValue("Groups");
            if (g2 != null)
            {
                var groups = g2.AttemptedValue.Split(',');
                foreach (var group in groups)
                {
                    var a = group.Split('=');
                    model.Groups.Add(new GroupObject(a[1], a[0]));
                }
            }
            return model;
        }
    }
}