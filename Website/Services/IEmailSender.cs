using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string[] email, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string bcc = null, string from = null);
        Task SendBulkEmailAsync(List<MyVoltage.Data.Log_Notification> emails, DbContextOptions<MyVoltageDbContext> options, string subject, string message, string htmlMessage, byte[] fileBytes = null, string fileName = null, string contentType = null, string cc = null, string from = null);
    }
}
