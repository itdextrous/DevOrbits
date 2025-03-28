using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltageBLL
{
    public class CEmail
    {
        public class MyEmailAttachment
        {
            public byte[] FileBytes { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
        }
        public static void SendEmail(List<string> email, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string bcc = null, string from = null)
        {
            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(MyVoltageBLL.Application)).CodeBase).Remove(0, 6);
            if (dllPath[1] != ':') dllPath = "\\\\" + dllPath;
            if (dllPath.ToUpper().Contains("DEBUG"))
            {
                email = new List<string>()
                {
                    "lendl@myvoltage.co.za"
                };
                subject = "(local test) " + subject;
            }



            RestClient client = new RestClient();
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
                request.AddParameter("from", "No Reply <no-reply@mymetersa.co.za>");
            }


            if (cc != null)
            {
                request.AddParameter("cc", cc);
            }

            if (bcc != null)
            {
                request.AddParameter("bcc", bcc);
            }
            request.AddParameter("to", string.Join(",", email));
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

        public static void SendEmailBulk(List<string> email, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string bcc = null, string from = null)
        {
            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(MyVoltageBLL.Application)).CodeBase).Remove(0, 6);
            if (dllPath[1] != ':') dllPath = "\\\\" + dllPath;
            if (dllPath.ToUpper().Contains("DEBUG"))
            {
                email = new List<string>()
                {
                    "lendl@myvoltage.co.za"
                };
                subject = "(local test) " + subject;
            }



            RestClient client = new RestClient();
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
                request.AddParameter("from", "No Reply <no-reply@mymetersa.co.za>");
            }


            if (cc != null)
            {
                request.AddParameter("cc", cc);
            }

            if (bcc != null)
            {
                request.AddParameter("bcc", bcc);
            }
            request.AddParameter("to", string.Join(",", email));
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

        public static void SendEmail(List<string> email, string subject, string message, string htmlMessage, List<MyEmailAttachment> myEmailAttachments, string cc = null, string bcc = null)
        {
            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(MyVoltageBLL.Application)).CodeBase).Remove(0, 6);
            if (dllPath[1] != ':') dllPath = "\\\\" + dllPath;
            if (dllPath.ToUpper().Contains("DEBUG"))
            {
                email = new List<string>()
                {
                    "lendl@myvoltage.co.za"
                };
                subject = "(local test)" + subject;
            }

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api", "ec6d1d03add6234a240a87d13be6b329-3b1f59cf-f1fe5ba7");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mymetersa.co.za", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";

            //TODO: when changing from address, also add the sender header
            request.AddParameter("from", "My Voltage Reporting <info@mymetersa.co.za>");

            if (cc != null)
            {
                request.AddParameter("cc", cc);
            }

            if (bcc != null)
            {
                request.AddParameter("bcc", bcc);
            }

            request.AddParameter("to", string.Join(",", email));
            request.AddParameter("subject", subject);

            if (message != String.Empty)
            {
                request.AddParameter("text", message);
            }

            if (htmlMessage != String.Empty)
            {
                request.AddParameter("html", htmlMessage);
            }

            foreach (var attach in myEmailAttachments)
                request.AddFileBytes("attachment", attach.FileBytes, attach.FileName, attach.ContentType);

            request.Method = Method.POST;

            IRestResponse response = client.Execute(request);

        }

        public static void SendErrorEmail(Exception ex, string applicationNamespace, List<string> extraEmails = null)
        {
            string fullException = $"{ex.ToString()}{Environment.NewLine}<br />{ex.StackTrace}";

            if (ex.InnerException != null)
                fullException += $"{Environment.NewLine}<br />INNER:{Environment.NewLine}<br />{ex.InnerException.ToString()}{Environment.NewLine}<br />{ex.InnerException.StackTrace}";

            List<string> errorEmailAddr = new List<string>()
                    {
                        "lendl@myvoltage.co.za",
                        "nic@myvoltage.co.za"
                    };

            if (extraEmails != null && extraEmails.Count > 0)
                errorEmailAddr.AddRange(extraEmails);

            CEmail.SendEmail(errorEmailAddr, $"{applicationNamespace} Error", fullException, fullException, from: "errors@mymetersa.co.za");
        }
    }
}
