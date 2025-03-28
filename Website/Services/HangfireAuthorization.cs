//using Hangfire.Annotations;
//using Hangfire.Dashboard;
//using Microsoft.AspNetCore.Http;
//using MyVoltage.Data;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MyVoltage.Services
//{
//    public class HangfireAuthorization : IDashboardAuthorizationFilter
//    {
//        public bool Authorize(DashboardContext context)
//        {
//            var httpContext = context.GetHttpContext();

//            // Allow all authenticated users to see the Dashboard (potentially dangerous).

//            if (httpContext.User.IsInRole(UserRoleEnum.Operational.ToString()))
//                return true;
//            else
//                return false;

//            return httpContext.User.Identity.IsAuthenticated;
//        }
//    }
//}
