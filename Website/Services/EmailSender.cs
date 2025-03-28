using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltageApi.Data;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string[] email, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string bcc = null, string from = null)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api", "ec6d1d03add6234a240a87d13be6b329-3b1f59cf-f1fe5ba7");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mymetersa.co.za", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";

            if (from != null)
            {
                request.AddParameter("from", from);
                request.AddParameter("sender", from);

            }
            else
            {
                //TODO: when changing from address, also add the sender header
                request.AddParameter("sender", "My Voltage <info@myvoltage.co.za>");
                request.AddParameter("from", "My Voltage <info@myvoltage.co.za>");
            }

            if (cc != null)
            {
                request.AddParameter("cc", cc);
            }

            if (bcc != null)
            {
                request.AddParameter("bcc", bcc);
            }

            if (email.Length > 1)
                request.AddParameter("to", string.Join(",", email));
            else
                request.AddParameter("to", email[0]);

            request.AddParameter("subject", subject);

            if (message != String.Empty)
            {
                request.AddParameter("text", message);
            }

            if (htmlMessage != String.Empty)
            {
                request.AddParameter("html", htmlMessage);
            }

            if (fileBytes != null)
            {
                if (fileName != String.Empty)
                {
                    if (contentType != String.Empty)
                    {
                        request.AddFileBytes("attachment", fileBytes, fileName, contentType);
                    }
                }
            }

            request.Method = Method.POST;

            IRestResponse response = client.Execute(request);

        }

        public async Task SendBulkEmailAsync(List<MyVoltage.Data.Log_Notification> emails, DbContextOptions<MyVoltageDbContext> options, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string from = null)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api", "ec6d1d03add6234a240a87d13be6b329-3b1f59cf-f1fe5ba7");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mymetersa.co.za", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";

            if (from != null)
            {
                request.AddParameter("from", from);
                request.AddParameter("sender", from);
            }
            else
            {
                //TODO: when changing from address, also add the sender header
                request.AddParameter("sender", "My Voltage <info@myvoltage.co.za>");
                request.AddParameter("from", "My Voltage <info@myvoltage.co.za>");
            }

            if (cc != null)
            {
                request.AddParameter("cc", cc);
            }

            request.AddParameter("to", "noreply@mymetersa.co.za");

            List<string> emailAddr = new List<string>()
            {
                //"lendl@lendl.co.za",
                //"a@lendl.co.za",
                //"food@lendl.co.za",
                //"wood@lendl.co.za",
                //"stone@lendl.co.za",
                //"iron@lendl.co.za",
                //"lendl@myvoltage.co.za",
                //"rose@myvoltage.co.za",
                //"riaan@myvoltage.co.za",
                //"madelyn@myvoltage.co.za",
            };

            emailAddr = emails.Select(p => p.Recipients).ToList();

            request.AddParameter("bcc", string.Join(",", emailAddr));

            //foreach (var emailAddr in emails)
            //    request.AddParameter("bcc", emailAddr.Recipients);

            request.AddParameter("subject", subject);

            if (message != String.Empty)
            {
                request.AddParameter("text", message);
            }

            if (htmlMessage != String.Empty)
            {
                request.AddParameter("html", htmlMessage);
            }

            if (fileBytes != null)
            {
                if (fileName != String.Empty)
                {
                    if (contentType != String.Empty)
                    {
                        request.AddFileBytes("attachment", fileBytes, fileName, contentType);
                    }
                }
            }

            request.Method = Method.POST;

            IRestResponse response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Create Notification Logs for each email addr

                MyVoltageDbContext db = new MyVoltageDbContext(options);

                db.Log_Notifications.AddRange(emails);
                db.SaveChanges();
            }

        }


    }
}
