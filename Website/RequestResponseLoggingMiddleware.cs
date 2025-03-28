using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage
{
    public class LogRequestMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<LogRequestMiddleware> _logger;
        private Func<string, Exception, string> _defaultFormatter = (state, exception) => state;
        private readonly string _connection;

        public LogRequestMiddleware(RequestDelegate next, ILogger<LogRequestMiddleware> logger, string connection)
        {
            this.next = next;
            _logger = logger;
            _connection = connection;
        }

        public async Task Invoke(HttpContext context)
        {
            DateTime startTime = DateTime.Now;
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            var url = UriHelper.GetDisplayUrl(context.Request);
            var requestBodyText = new StreamReader(requestBodyStream).ReadToEnd();
            try
            {
                SqlConnection conn = new SqlConnection(_connection);
                conn.Open();

                SqlCommand sqlCommand = new SqlCommand("sp_InsertLog", conn);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@SourceApplication", "MyVoltage");
                sqlCommand.Parameters.AddWithValue("@Started", DateTime.Now);
                sqlCommand.Parameters.AddWithValue("@URL", HttpUtility.UrlEncode($"{context.Request.Host}{context.Request.Path}"));
                sqlCommand.Parameters.AddWithValue("@Method", context.Request.Method);
                sqlCommand.Parameters.AddWithValue("@Protocol", context.Request.Protocol);
                sqlCommand.Parameters.AddWithValue("@Request", HttpUtility.HtmlEncode(requestBodyText));
                sqlCommand.Parameters.AddWithValue("@SourceIP", context.Connection.RemoteIpAddress.ToString());

                int ResultID = (int)sqlCommand.ExecuteScalar();

                conn.Close();
                context.Session.SetInt32("LogID", ResultID);
            }
            catch { }

            //_logger.Log(LogLevel.Information, 1, $"REQUEST METHOD: {context.Request.Method}, REQUEST BODY: {requestBodyText}, REQUEST URL: {url}", null, _defaultFormatter);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await next(context);
            context.Request.Body = originalRequestBody;
        }
    }

    public class LogResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogResponseMiddleware> _logger;
        private Func<string, Exception, string> _defaultFormatter = (state, exception) => state;
        private readonly string _connection;

        public LogResponseMiddleware(RequestDelegate next, ILogger<LogResponseMiddleware> logger, string connection)
        {
            _next = next;
            _logger = logger;
            _connection = connection;
        }

        public async Task Invoke(HttpContext context)
        {
            var bodyStream = context.Response.Body;

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(responseBodyStream).ReadToEnd();

            try
            {
                SqlConnection conn = new SqlConnection(_connection);
                conn.Open();

                SqlCommand updateCommand = new SqlCommand("sp_CompleteLog", conn);
                updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                updateCommand.Parameters.AddWithValue("@ID", (int)context.Session.GetInt32("LogID"));
                updateCommand.Parameters.AddWithValue("@Ended", DateTime.Now);
                updateCommand.Parameters.AddWithValue("@DurationMS", "0");// (DateTime.Now - startTime).TotalMilliseconds);
                updateCommand.Parameters.AddWithValue("@Response", HttpUtility.HtmlEncode(responseBody));
                updateCommand.Parameters.AddWithValue("@StatusCode", context.Response.StatusCode);

                updateCommand.ExecuteNonQuery();

                conn.Close();
            }
            catch { }


            //_logger.Log(LogLevel.Information, 1, $"RESPONSE LOG: {responseBody}", null, _defaultFormatter);
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(bodyStream);
        }
    }
}
