using Microsoft.AspNetCore.Mvc;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MVApiController : Controller
    {
        private readonly IEmailSender _emailSender;

        public MVApiController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/api/testmail")]
        public ActionResult TestMail()
        {
            return Content("");

            StringBuilder response = new StringBuilder();

            try
            {
                foreach (var frm in Request.Form)
                {
                    response.AppendLine($"Form:{frm.Key} - {string.Join(",", frm.Value.Select(p => p))}");
                }

            }
            catch { }

            try
            {
                foreach (var frm in Request.Query)
                {
                    response.AppendLine($"Query:{frm.Key} - {string.Join(",", frm.Value.Select(p => p))}");
                }

            }
            catch { }

            try
            {
                foreach (var frm in Request.Headers)
                {
                    response.AppendLine($"Header:{frm.Key} - {string.Join(",", frm.Value.Select(p => p))}");
                }

            }
            catch { }

            try
            {
                System.IO.StreamReader streamReader = new System.IO.StreamReader(Request.Body);

                response.AppendLine($"Body:{streamReader.ReadToEnd()}");

            }
            catch { }

            return Content("true");
        }

    }
}
