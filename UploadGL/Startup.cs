using Microsoft.Owin;
using Owin;
using Hangfire;
using Hangfire.SqlServer;
using System;
using System.Web;
using Hangfire.Dashboard;
using System.Diagnostics;
using System.Collections.Generic;
using Hangfire.MemoryStorage;

[assembly: OwinStartup(typeof(UploadGL.Startup))]

namespace UploadGL
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration.UseMemoryStorage(new MemoryStorageOptions { FetchNextJobTimeout = TimeSpan.FromHours(24) });
            var options = new DashboardOptions
            {
                AppPath = VirtualPathUtility.ToAbsolute("~")//,

                //  IsReadOnlyFunc = (DashboardContext context) => true
            };
            app.UseHangfireDashboard("/hangfire", options);
            app.UseHangfireServer();
        }
    }
}
