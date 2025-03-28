using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using MyVoltage.Services;

namespace MyVoltage.Services
{
    public static class EmailSenderExtensions
    {

        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(new string[] { email }, "Confirm your email",
                 $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>", $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }

        public static Task SendUserDetailsAsync(this IEmailSender emailSender, string email, string details, string customerNo, string customerEmail)
        {
            return emailSender.SendEmailAsync(new string[] { email }, "New Registration " + customerNo,
                 details, details, from: customerEmail);
        }

        public static Task SendResetPasswordEmailAsync(this IEmailSender emailSender, string email, string details)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "reset_password.txt");
            string htmlEmail = System.IO.File.ReadAllText(file);

            htmlEmail = htmlEmail.Replace("{details}", details);
            htmlEmail = htmlEmail.Replace("{url}", "https://www.mymetersa.co.za");

            return emailSender.SendEmailAsync(new string[] { email }, "Reset Password",
                htmlEmail, htmlEmail, from: "No Reply <no-reply@mymetersa.co.za>");
        }

        public static Task SendResetPasswordEmailNewAsync(this IEmailSender emailSender, string email, string details, string customerName = "")
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "reset_password_new.txt");
            string htmlEmail = System.IO.File.ReadAllText(file);

            htmlEmail = htmlEmail.Replace("{details}", details);
            htmlEmail = htmlEmail.Replace("{name}", customerName);
            htmlEmail = htmlEmail.Replace("{url}", "https://www.mymetersa.co.za");

            return emailSender.SendEmailAsync(new string[] { email }, "Reset Password",
                htmlEmail, htmlEmail, from: "No Reply <no-reply@mymetersa.co.za>");
        }

        public static Task SendContactEmailAsync(this IEmailSender emailSender, string email, string details, string customerNo, string customerEmail, byte[] fileBytes, string fileName, string contentType)
        {
            return emailSender.SendEmailAsync(new string[] { email }, "Contact Email " + customerNo,
                details, details, from: customerEmail, fileBytes: fileBytes, fileName: fileName, contentType: contentType);
        }

        public static Task SendNotificationEmail(this IEmailSender emailSender, string email, string name, string subject, string template, string balance, string serial, string userEmail)
        {

            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", template);
            string htmlEmail = System.IO.File.ReadAllText(file);

            string url = "http://www.mymetersa.co.za";
            htmlEmail = htmlEmail.Replace("{name}", name);
            if (balance != null)
            {
                htmlEmail = htmlEmail.Replace("{balance}", balance);
            }

            htmlEmail = htmlEmail.Replace("{serial}", serial);

            htmlEmail = htmlEmail.Replace("{url}", url);

            return emailSender.SendEmailAsync(new string[] { userEmail }, subject,
                htmlEmail, htmlEmail, null, "", "", email, "lendl@myvoltage.co.za");

        }

        public static Task SendEmailCodeAsync(this IEmailSender emailSender, string email, string code, string name, string Id, IHttpContextAccessor context)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "comfirmation_code_new.txt");
            string htmlEmail = System.IO.File.ReadAllText(file);

            var request = context.HttpContext.Request;

            string url = string.Concat(
                        request.Scheme,
                        "://",
                        request.Host.ToUriComponent(),
                        request.PathBase.ToUriComponent());

            htmlEmail = htmlEmail.Replace("{code}", code);
            htmlEmail = htmlEmail.Replace("{name}", name);
            htmlEmail = htmlEmail.Replace("{url}", url);
            htmlEmail = htmlEmail.Replace("{Id}", Id);

            return emailSender.SendEmailAsync(new string[] { email }, "Confirm your registration",
                $"Your My Voltage confirmation code is  {code}. Enter this code during registration to verify your contact details. Kind Regards The My Voltage Team", htmlEmail);
        }

        public static Task SendExternalChargesUploadNotification(this IEmailSender emailSender, MyVoltage.Data.ExternalChargesSchedulingImport importItem)
        {
            // Upload Email
            // Please find attached external charges scheduled for processing.
            // Item detail (copy cols from excel)

            #region Azure Download

            string shareName = "j-finance-externalcharges";
            string companyDirname = importItem.UploadURL.Split('/')[0];
            string customerDirname = importItem.UploadURL.Split('/')[1];
            string filename = Path.GetFileName(importItem.UploadURL);

            Stream memoryStream = new MemoryStream();
            ShareClient share = new ShareClient("DefaultEndpointsProtocol=https;AccountName=myvoltagezaguestdiag;AccountKey=gwxVtJilATUf9N/UnSRH63yRoKrnq2uZ3g4ewZh+8/ycnaC1xUx5CRhSgiivBnBPArnwHki4haWqeCLcNQ0R/A==;EndpointSuffix=core.windows.net", shareName);
            ShareDirectoryClient directoryCompany2 = share.GetDirectoryClient(companyDirname.ToLower());
            if (directoryCompany2.Exists())
            {
                ShareDirectoryClient directory = directoryCompany2.GetSubdirectoryClient(customerDirname.ToLower());
                if (directory.Exists())
                {
                    ShareFileClient file = directory.GetFileClient(filename.ToLower());

                    if (file.Exists())
                    {
                        // Download the file
                        ShareFileDownloadInfo download = file.Download();
                        download.Content.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                    }
                }
            }

            byte[] fileContents = new byte[memoryStream.Length];
            memoryStream.Position = 0;
            memoryStream.Read(fileContents, 0, fileContents.Length);

            #endregion

            var itemDetail = new
            {
                importItem.CreatedDate,
                importItem.SkybillCustomerNo,
                importItem.PostingDate,
                importItem.ReferenceNumber,
                importItem.Amount,
                importItem.UploadConfirmationEmail,
                importItem.ClientConfirmationEmail,
                importItem.DateScheduleStarted,
                importItem.DateScheduleEnded
            };

            StringBuilder sbEmail = new StringBuilder();
            sbEmail.AppendLine("Please find attached external charges scheduled for processing<br /><br />");

            foreach (PropertyInfo p in itemDetail.GetType().GetProperties())
            {
                sbEmail.AppendLine($"{p.Name}: {p.GetValue(itemDetail, null)}<br />");
            }


            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(importItem.UploadURL, out contentType))
            {
                contentType = "application/octet-stream";
            }


            return emailSender.SendEmailAsync(new string[] { importItem.UploadConfirmationEmail }, $"External Charges - {importItem.SkybillCustomerNo} - {importItem.ReferenceNumber}",
               sbEmail.ToString(), sbEmail.ToString(), fileContents, Path.GetFileName(importItem.UploadURL), contentType, from: "No Reply <no-reply@mymetersa.co.za>");
        }

        public static Task SendNotificationEmailCodeAsync(this IEmailSender emailSender, string email, string code, string name, IHttpContextAccessor context)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "email_comfirmation_code.txt");
            string htmlEmail = System.IO.File.ReadAllText(file);

            var request = context.HttpContext.Request;

            string url = string.Concat(
                        request.Scheme,
                        "://",
                        request.Host.ToUriComponent(),
                        request.PathBase.ToUriComponent());

            htmlEmail = htmlEmail.Replace("{code}", code);
            htmlEmail = htmlEmail.Replace("{name}", name);

            return emailSender.SendEmailAsync(new string[] { email }, "Confirm your notification email",
                $"Your My Voltage notification email confirmation code is  {code}. Enter this code to verify your Notification email detail. Kind Regards The My Voltage Team", htmlEmail);
        }

    }
}
