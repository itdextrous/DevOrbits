using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class FTPProvider
    {
        private static string FTPUserName = "photouploader";
        private static string FTPPassword = "mRdgMQC3Tw7j";
        private static string FTPRootURL = "ftp://automation.mymetersa.co.za/";

        public static string MakeSafeFileName(string source)
        {
            foreach (char ch in Path.GetInvalidFileNameChars())
                source = source.Replace(ch.ToString(), string.Empty);

            foreach (char ch in Path.GetInvalidPathChars())
                source = source.Replace(ch.ToString(), string.Empty);

            source = source.Replace(".", string.Empty);
            source = source.Replace("/", string.Empty);
            source = source.Replace("-", string.Empty);

            return source;
        }

        public static void CreateFolderIfNotExist(string folderName, string username = "", string password = "")
        {
            if (!string.IsNullOrEmpty(username))
                FTPUserName = username;
            if (!string.IsNullOrEmpty(password))
                FTPPassword = password;

            if (folderName.StartsWith('/'))
                folderName = folderName.Remove(0, 1);
            if (!folderName.EndsWith('/'))
                folderName = folderName + "/";

            string dirUrl = FTPRootURL + folderName;

            try
            {
                //create the directory
                FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(dirUrl);
                requestDir.Method = WebRequestMethods.Ftp.MakeDirectory;
                requestDir.Credentials = new NetworkCredential(FTPUserName, FTPPassword);
                requestDir.UsePassive = true;
                requestDir.UseBinary = true;
                requestDir.KeepAlive = false;
                requestDir.EnableSsl = true;

                FtpWebResponse dirresponse = (FtpWebResponse)requestDir.GetResponse();
                Stream ftpStream = dirresponse.GetResponseStream();

                ftpStream.Close();
                dirresponse.Close();
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    response.Close();
                }
                else
                {
                    response.Close();
                }
            }

        }

        public static void UploadFile(string foldername, string filename, byte[] fileBytes, string username = "", string password = "")
        {
            if (!string.IsNullOrEmpty(username))
                FTPUserName = username;
            if (!string.IsNullOrEmpty(password))
                FTPPassword = password;



            CreateFolderIfNotExist(foldername, username, password);

            string uploadURL = $"{FTPRootURL}/{foldername}/{filename}";
            Console.WriteLine(uploadURL);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uploadURL);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.EnableSsl = true;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(FTPUserName, FTPPassword);

            request.ContentLength = fileBytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileBytes, 0, fileBytes.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }
        }

        public static List<string> GetFilesInFolder(string foldername, string username = "", string password = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(username))
                    FTPUserName = username;
                if (!string.IsNullOrEmpty(password))
                    FTPPassword = password;

                string uploadURL = $"{FTPRootURL}/{foldername}";
                Console.WriteLine(uploadURL);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uploadURL);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.EnableSsl = true;
                request.Credentials = new NetworkCredential(FTPUserName, FTPPassword);

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                List<string> files = new List<string>();

                string line = reader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    files.Add(line);
                    line = reader.ReadLine();
                }

                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new List<string>();
            }
        }

        public static Stream DownloadFile(string filename, string username = "", string password = "")
        {
            if (!string.IsNullOrEmpty(username))
                FTPUserName = username;
            if (!string.IsNullOrEmpty(password))
                FTPPassword = password;

            if (filename.StartsWith("/"))
                filename = filename.Remove(0, 1);

            string uploadURL = $"{FTPRootURL}/{filename}";
            Console.WriteLine(uploadURL);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uploadURL);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.EnableSsl = true;
            request.Credentials = new NetworkCredential(FTPUserName, FTPPassword);

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(responseStream);

                return responseStream;
            }
            catch
            {
                return null;
            }
        }

    }
}
