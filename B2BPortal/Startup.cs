using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security.DataProtection;
using B2BPortal.Infrastructure;
using B2BPortal.Common.Utils;

[assembly: OwinStartup(typeof(B2BPortal.Startup))]

namespace B2BPortal
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var owinDPP = app.GetDataProtectionProvider();
            //Utils.OwinDPP = owinDPP.Create();

            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888
            ConfigureAuth(app);
        }
    }
}
