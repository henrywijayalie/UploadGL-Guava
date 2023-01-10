using Hangfire;
using Hangfire.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using UploadGL.Helpers;

namespace UploadGL
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });
            LogProvider.SetCurrentLogProvider(null);
            log4net.Config.XmlConfigurator.Configure();

            TextBuffer.WriteLine("Application started.");
        }
    }
}
