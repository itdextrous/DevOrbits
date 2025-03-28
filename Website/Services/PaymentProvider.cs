using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using MyVoltage.Models.PaymentViewModels;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using SalesJournal;

namespace MyVoltage.Services
{
    public class PaymentProvider
    {

        private string _sagepayNowServiceKey;
        private string _sagePayMyMeterSAServiceKey;
        private DbContextOptions<MyVoltageDbContext> _options;

        public PaymentProvider(IConfiguration config, DbContextOptions<MyVoltageDbContext> options)
        {
            _sagepayNowServiceKey = config["SagePay:SagePayNowServiceKey"];
            _sagePayMyMeterSAServiceKey = config["SagePay:SagePayMyMeterSAServiceKey"];
            _options = options;
        }
        public PaymentProvider()
        {
            //_sagepayNowServiceKey = config["SagePay:SagePayNowServiceKey"];
            //_options = options;
        }

        public Task<ServiceReference1.Create_Result> CreateJournalEntry(decimal amount, string accountNo, Company company, Customer customer, string description)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                ServiceReference1.CashReceiptJournal journal = new ServiceReference1.CashReceiptJournal();
                journal.Posting_DateSpecified = true;
                journal.Posting_Date = DateTime.UtcNow.Date;
                journal.Document_TypeSpecified = true;
                journal.Document_Type = ServiceReference1.Document_Type.Payment;
                journal.Account_TypeSpecified = true;
                journal.Account_Type = ServiceReference1.Account_Type.Customer;
                journal.Account_No = customer.CustomerNumber;
                journal.AmountSpecified = true;
                journal.Description = description;
                journal.Amount = amount * -1;
                journal.Bal_Account_TypeSpecified = true;
                journal.Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account;

                journal.Bal_Account_No = accountNo;

                ServiceReference1.Create create = new ServiceReference1.Create("DEFAULT", journal);

                return client.CreateAsync(create);
            }
        }

        public Task<ServiceReference1.Create_Result> CreateJournalEntry(Company company, ServiceReference1.CashReceiptJournal journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                ServiceReference1.Create create = new ServiceReference1.Create("DEFAULT", journal);

                return client.CreateAsync(create);
            }
        }

        public ServiceReference1.Create_Result CreateJournalEntry2(Company company, ServiceReference1.CashReceiptJournal journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                ServiceReference1.Create create = new ServiceReference1.Create("DEFAULT", journal);

                return client.CreateAsync(create).Result;
            }
        }

        public Task<ServiceReference1.GetRecIdFromKey_Result> GetRecIdFromKey(ServiceReference1.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.CashReceiptJournal;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key);
            }
        }

        public ServiceReference1.GetRecIdFromKey_Result GetRecIdFromKey2(ServiceReference1.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.CashReceiptJournal;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key).Result;
            }
        }

        public Task<ServiceReference2.PostReceiptJournal_Result> PostReceiptJournalAsync(ServiceReference1.GetRecIdFromKey_Result recIdFromKey, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference2.WSManagement_PortClient client = new ServiceReference2.WSManagement_PortClient();

            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            string[] strArr = recIdFromKey.GetRecIdFromKey_Result1.Split(',');

            string jnlTemplateNameStr = strArr[0].ToString();

            string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

            string jnlTemplateName = jnlTemplateNameStrArr[1];

            string jnlBatchName = strArr[1];
            int lineNo = Int32.Parse(strArr[2]);

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo);
            }
        }
        public ServiceReference2.PostReceiptJournal_Result PostReceiptJournalAsync2(ServiceReference1.GetRecIdFromKey_Result recIdFromKey, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference2.WSManagement_PortClient client = new ServiceReference2.WSManagement_PortClient();

            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            string[] strArr = recIdFromKey.GetRecIdFromKey_Result1.Split(',');

            string jnlTemplateNameStr = strArr[0].ToString();

            string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

            string jnlTemplateName = jnlTemplateNameStrArr[1];

            string jnlBatchName = strArr[1];
            int lineNo = Int32.Parse(strArr[2]);

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo).Result;
            }
        }

        public Task<SalesJournal.Create_Result> CreateSalesJournalEntry(decimal amount, Company company, MyVoltage.Data.Customer customer, string description)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

            SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                SalesJournal.SalesJnl journal = new SalesJournal.SalesJnl();
                journal.Posting_DateSpecified = true;
                journal.Posting_Date = DateTime.UtcNow.Date;
                journal.Document_TypeSpecified = true;
                journal.Document_Type = SalesJournal.Document_Type.Invoice;
                journal.Account_TypeSpecified = true;
                journal.Account_Type = SalesJournal.Account_Type.Customer;
                journal.Account_No = customer.CustomerNumber;
                journal.AmountSpecified = true;
                journal.Description = description;
                journal.Amount = amount;
                journal.Bal_Account_TypeSpecified = true;
                journal.Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account;

                journal.Bal_Account_No = "6810";

                SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                return client.CreateAsync(create);
            }
        }

        public Task<SalesJournal.GetRecIdFromKey_Result> GetSalesRecIdFromKey(SalesJournal.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

            SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.SalesJnl;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key);
            }
        }

        public SalesJournal.GetRecIdFromKey_Result GetSalesRecIdFromKey2(SalesJournal.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

            SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.SalesJnl;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key).Result;
            }
        }

        public Task<ServiceReference2.PostReceiptJournal_Result> PostSalesReceiptJournalAsync(SalesJournal.GetRecIdFromKey_Result recIdFromKey, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference2.WSManagement_PortClient client = new ServiceReference2.WSManagement_PortClient();

            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            string[] strArr = recIdFromKey.GetRecIdFromKey_Result1.Split(',');

            string jnlTemplateNameStr = strArr[0].ToString();

            string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

            string jnlTemplateName = jnlTemplateNameStrArr[1];

            string jnlBatchName = strArr[1];
            int lineNo = Int32.Parse(strArr[2]);

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo);
            }
        }
        public ServiceReference2.PostReceiptJournal_Result PostSalesReceiptJournalAsync2(SalesJournal.GetRecIdFromKey_Result recIdFromKey, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ServiceReference2.WSManagement_PortClient client = new ServiceReference2.WSManagement_PortClient();

            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            string[] strArr = recIdFromKey.GetRecIdFromKey_Result1.Split(',');

            string jnlTemplateNameStr = strArr[0].ToString();

            string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

            string jnlTemplateName = jnlTemplateNameStrArr[1];

            string jnlBatchName = strArr[1];
            int lineNo = Int32.Parse(strArr[2]);

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo).Result;
            }
        }

        public Task<SalesJournal.Create_Result> CreateSalesJournalEntry(Company company, SalesJournal.SalesJnl journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

            SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                return client.CreateAsync(create);
            }
        }

        public SalesJournal.Create_Result CreateSalesJournalEntry2(Company company, SalesJournal.SalesJnl journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

            SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);

                return client.CreateAsync(create).Result;
            }
        }

        public Task<GeneralJournalReference.Create_Result> CreateGeneralJournalEntry(Company company, GeneralJournalReference.GeneralJournal journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/GeneralJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            GeneralJournalReference.GeneralJournal_PortClient client = new GeneralJournalReference.GeneralJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                GeneralJournalReference.Create create = new GeneralJournalReference.Create("DEFAULT", journal);

                return client.CreateAsync(create);
            }

        }
        public GeneralJournalReference.Create_Result CreateGeneralJournalEntry2(Company company, GeneralJournalReference.GeneralJournal journal)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/GeneralJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            GeneralJournalReference.GeneralJournal_PortClient client = new GeneralJournalReference.GeneralJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);

            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication()
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                GeneralJournalReference.Create create = new GeneralJournalReference.Create("DEFAULT", journal);

                return client.CreateAsync(create).Result;
            }

        }

        public Task<GeneralJournalReference.GetRecIdFromKey_Result> GetGeneralJournalRecIdFromKey(GeneralJournalReference.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/GeneralJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            GeneralJournalReference.GeneralJournal_PortClient client = new GeneralJournalReference.GeneralJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.GeneralJournal;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key);
            }
        }

        public GeneralJournalReference.GetRecIdFromKey_Result GetGeneralJournalRecIdFromKey2(GeneralJournalReference.Create_Result createResult, Company company)
        {
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company.Name}/Page/GeneralJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";

            GeneralJournalReference.GeneralJournal_PortClient client = new GeneralJournalReference.GeneralJournal_PortClient();
            //  Set the user’s credentials on the proxy  
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            var result = createResult.GeneralJournal;

            using (new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                return client.GetRecIdFromKeyAsync(result.Key).Result;
            }
        }


        public PaymentViewModel GetPaymentModel(decimal amount, string referenceID, ApplicationUser user, string isCompanyAdminRecharge)
        {
            using (var db = new MyVoltageDbContext(_options))
            {
                var customer = (from tbl in db.Customers
                                where tbl.UserID == user.Id
                                where tbl.IsDeleted == false
                                select tbl).FirstOrDefault();

                var company = (from tbl in db.Companies
                               where tbl.CompanyID == customer.CompanyID
                               select tbl).FirstOrDefault();

                var sagepayNowServiceKey = company.ServiceKey;
                if (sagepayNowServiceKey == null || sagepayNowServiceKey == String.Empty)
                {
                    sagepayNowServiceKey = _sagepayNowServiceKey;
                }

                if (!string.IsNullOrEmpty(isCompanyAdminRecharge))
                    sagepayNowServiceKey = _sagePayMyMeterSAServiceKey;

                return new PaymentViewModel
                {
                    m1 = sagepayNowServiceKey,
                    p2 = referenceID,
                    p3 = referenceID.ToString(),
                    p4 = amount.ToString("0.##"),
                    Budget = "N",
                    m4 = company.Name,
                    m5 = customer.CustomerNumber, 
                    m6 = "",
                    m9 = user.Email,
                    m10 = ""
                };
            }
        }

        public TransactionStatus IsSuccessfulPayment(PaymentNotifyModel model, Payment payment)
        {
            if (payment.PaymentID.ToString() != model.Reference)
                return TransactionStatus.Hacked;

            if (model.TransactionAccepted == "false")
            {
                return TransactionStatus.Declined;
            }

            return TransactionStatus.Approved;
        }

        internal Task GetRecIdFromKey(Create_Result journalEntryVending, Company item2)
        {
            throw new NotImplementedException();
        }
    }

    public enum TransactionStatus : int
    {
        Incomplete = 1,
        Approved = 2,
        Declined = 3,
        Hacked = 4,
    }
}

