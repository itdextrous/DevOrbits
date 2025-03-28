using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Api.SageAccounting
{
    public class SageAccountingAPI : ApiClient
    {
        public string _baseUrl = "https://accounting.sageone.co.za/api/2.0.0";

        public IMemoryCache _cache;
        public DbContextOptions<Data.MyVoltageDbContext> _options;
        public DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private const string username = "nic@myvoltage.co.za";
        private const string password = "MyVoltage1*";
        private const string token = "8279D680-6CC8-47C3-8ED6-AEAFDAC14D3B";

        public SageAccountingAPI(IMemoryCache cache, DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            _cache = cache;
            _options = options;
            _APIoptions = APIoptions;
        }

        #region Shared Methods / API Calls

        public T Get<T>(string method, string filter, int offset = 0)
        {
            string offsetStr = "";

            if (offset > 0)
            {
                offsetStr = $"&$skip={offset}";
            }

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/{method}?apikey={token}{filter}{offsetStr}");
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}", password));

            HttpWebResponse myHttpWebResponse = null;

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Console.WriteLine(myHttpWebRequest.RequestUri.ToString());
            }
            catch (WebException we)
            {
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T Post<T, Y>(string method, Y obj, string filter, int offset = 0)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string offsetStr = "";

            if (offset > 0)
            {
                offsetStr = $"&$skip={offset}";
            }

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/{method}?apikey={token}{filter}{offsetStr}");
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}", password));

            HttpWebResponse myHttpWebResponse;

            string json = JsonConvert.SerializeObject(obj);
            byte[] byteArray = Encoding.ASCII.GetBytes(json);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{myHttpWebRequest.RequestUri.ToString()}{Environment.NewLine}{ex.ToString()}");
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex)
            {
                // remove []
                responseText = responseText.Remove(responseText.IndexOf('['), 1);
                responseText = responseText.Remove(responseText.LastIndexOf(']'), 1);
                return JsonConvert.DeserializeObject<T>(responseText);
            }



            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T DELETE<T>(string method, int companyID)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/{method}?apikey={token}{$"&CompanyID={companyID}"}");
            myHttpWebRequest.Method = "DELETE";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}", password));

            HttpWebResponse myHttpWebResponse = null;

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Console.WriteLine(myHttpWebRequest.RequestUri.ToString());
            }
            catch (WebException we)
            {
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        #endregion

        #region Class Declaration

        public class CompaniesResult
        {
            public int TotalResults { get; set; }
            public int ReturnedResults { get; set; }
            public Result[] Results { get; set; }
            public class Result
            {
                public int ID { get; set; }
                public string Name { get; set; }
                public string CurrencySymbol { get; set; }
                public int CurrencyDecimalDigits { get; set; }
                public int NumberDecimalDigits { get; set; }
                public string DecimalSeparator { get; set; }
                public int HoursDecimalDigits { get; set; }
                public int ItemCostPriceDecimalDigits { get; set; }
                public int ItemSellingPriceDecimalDigits { get; set; }
                public string PostalAddress1 { get; set; }
                public string PostalAddress2 { get; set; }
                public string PostalAddress3 { get; set; }
                public string PostalAddress4 { get; set; }
                public string PostalAddress5 { get; set; }
                public string GroupSeparator { get; set; }
                public int RoundingValue { get; set; }
                public int TaxSystem { get; set; }
                public int RoundingType { get; set; }
                public bool AgeMonthly { get; set; }
                public bool DisplayInactiveItems { get; set; }
                public bool WarnWhenItemCostIsZero { get; set; }
                public bool DoNotAllowProcessingIntoNegativeQuantities { get; set; }
                public bool WarnWhenItemQuantityIsZero { get; set; }
                public bool WarnWhenItemSellingBelowCost { get; set; }
                public int CountryId { get; set; }
                public bool EnableCustomerZone { get; set; }
                public bool EnableAutomaticBankFeedRefresh { get; set; }
                public string ContactName { get; set; }
                public string Telephone { get; set; }
                public string Fax { get; set; }
                public string Mobile { get; set; }
                public string Email { get; set; }
                public bool IsPrimarySendingEmail { get; set; }
                public string PostalAddress01 { get; set; }
                public string PostalAddress02 { get; set; }
                public string PostalAddress03 { get; set; }
                public string PostalAddress04 { get; set; }
                public string PostalAddress05 { get; set; }
                public string CompanyInfo01 { get; set; }
                public string CompanyInfo02 { get; set; }
                public string CompanyInfo03 { get; set; }
                public string CompanyInfo04 { get; set; }
                public string CompanyInfo05 { get; set; }
                public bool IsOwner { get; set; }
                public bool UseCCEmail { get; set; }
                public string CCEmail { get; set; }
                public int DateFormatId { get; set; }
                public bool CheckForDuplicateCustomerReferences { get; set; }
                public bool CheckForDuplicateSupplierReferences { get; set; }
                public string DisplayName { get; set; }
                public bool DisplayInactiveCustomers { get; set; }
                public bool DisplayInactiveSuppliers { get; set; }
                public bool DisplayInactiveTimeProjects { get; set; }
                public bool UseInclusiveProcessingByDefault { get; set; }
                public bool LockProcessing { get; set; }
                public bool LockTimesheetProcessing { get; set; }
                public int TaxPeriodFrequency { get; set; }
                public bool UseNoreplyEmail { get; set; }
                public bool AgeingBasedOnDueDate { get; set; }
                public bool UseLogoOnEmails { get; set; }
                public bool UseLogoOnCustomerZone { get; set; }
                public string City { get; set; }
                public string State { get; set; }
                public string Country { get; set; }
                public DateTime Created { get; set; }
                public DateTime Modified { get; set; }
                public bool Active { get; set; }
                public string TaxNumber { get; set; }
                public string RegisteredName { get; set; }
                public string RegistrationNumber { get; set; }
                public bool IsPracticeAccount { get; set; }
                public int LogoPositionID { get; set; }
                public string CompanyTaxNumber { get; set; }
                public string TaxOffice { get; set; }
                public string CustomerZoneGuid { get; set; }
                public int ClientTypeId { get; set; }
                public int DisplayTotalTypeId { get; set; }
                public bool DisplayInCompanyConsole { get; set; }
                public DateTime LastLoginDate { get; set; }
                public int TaxReportingTypeId { get; set; }
                public bool SalesOrdersReserveItemQuantities { get; set; }
                public bool PrescribedGoodsTrader { get; set; }
                public bool DisplayInactiveItemBundles { get; set; }
                public int CompanyTransferStatus { get; set; }
                public int InventoryTypeId { get; set; }
            }
        }

        public class DetailedLedgerTransactionsResult
        {
            public int TotalResults { get; set; }
            public int ReturnedResults { get; set; }
            public Result[] Results { get; set; }

            public class Rootobject
            {
                public int TotalResults { get; set; }
                public int ReturnedResults { get; set; }
                public Result[] Results { get; set; }
            }

            public class Result
            {
                public int ID { get; set; }
                public DateTime Date { get; set; }
                public int TransactionTypeId { get; set; }
                public string Reference { get; set; }
                public string Description { get; set; }
                public int AccountId { get; set; }
                public string AccountDescription { get; set; }
                public int ContraAccountId { get; set; }
                public string ContraAccountDescription { get; set; }
                public decimal Debit { get; set; }
                public decimal Credit { get; set; }
                public int TaxTypeId { get; set; }
                public string TaxTypeName { get; set; }
                public DateTime Modified { get; set; }
                public int AccountCategoryId { get; set; }
                public string AccountCategoryDescription { get; set; }
                public string TransactionTypeDescription { get; set; }
                public int AnalysisCategoryId1 { get; set; }
                public int AnalysisCategoryId2 { get; set; }
                public int AnalysisCategoryId3 { get; set; }
            }
        }

        public class DetailedLedgerTransactionsRequest
        {
            public bool Inactive { get; set; }
            public bool Active { get; set; }
            public string FromAccount { get; set; }
            public string ToAccount { get; set; }
            public object[] AccountCategorieIds { get; set; }
            public object[] TransactionTypes { get; set; }
            public bool ConsolidateAdditionalCost { get; set; }
            public bool IncludeNoTax { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
        }

        public class GetChartOfAccountsResult
        {
            public int TotalResults { get; set; }
            public int ReturnedResults { get; set; }
            public Result[] Results { get; set; }

            public class Result
            {
                public int ID { get; set; }
                public int SageID { get; set; }
                public string Name { get; set; }
                public AccountCategory Category { get; set; }
                public bool Active { get; set; }
                public decimal Balance { get; set; }
                public string Description { get; set; }
                public bool UnallocatedAccount { get; set; }
                public bool IsTaxLocked { get; set; }
                public DateTime Created { get; set; }
                public int AccountType { get; set; }
                public bool HasActivity { get; set; }
                public int DefaultTaxTypeId { get; set; }
                public Defaulttaxtype DefaultTaxType { get; set; }
            }
        }

        public class AccountCategoryResult
        {
            public int TotalResults { get; set; }
            public int ReturnedResults { get; set; }
            public AccountCategory[] Results { get; set; }
        }

        public class AccountCategory
        {
            public int ID { get; set; }
            public string Description { get; set; }
            public string Comment { get; set; }
            public int Order { get; set; }
        }

        public class Defaulttaxtype
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public decimal Percentage { get; set; }
            public bool IsDefault { get; set; }
            public bool HasActivity { get; set; }
            public bool IsManualTax { get; set; }
            public bool Active { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }
            public int CompanyId { get; set; }
        }

        public class AccountTaxTypesResult
        {
            public int TotalResults { get; set; }
            public int ReturnedResults { get; set; }
            public Result[] Results { get; set; }
            public class Result
            {
                public int ID { get; set; }
                public string Name { get; set; }
                public decimal Percentage { get; set; }
                public bool IsDefault { get; set; }
                public bool HasActivity { get; set; }
                public bool IsManualTax { get; set; }
                public bool Active { get; set; }
                public DateTime Created { get; set; }
                public int CompanyId { get; set; }
                public DateTime Modified { get; set; }
                public string TaxTypeDefaultUID { get; set; }
            }
        }

        public class SaveJournalEntryRequest
        {
            public string Date { get; set; }
            public int Effect { get; set; }
            public int AccountId { get; set; }
            public string Reference { get; set; }
            public string Description { get; set; }
            public int TaxTypeId { get; set; }
            public decimal Exclusive { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
            public int ContraAccountId { get; set; }
            public string Memo { get; set; }
            public bool HasAttachments { get; set; }
            public bool Editable { get; set; }
            public bool Locked { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
        }

        public class SaveJournalEntryResponse
        {
            public long ID { get; set; }
            public DateTime Date { get; set; }
            public int Effect { get; set; }
            public int AccountId { get; set; }
            public string Reference { get; set; }
            public string Description { get; set; }
            public int TaxTypeId { get; set; }
            public decimal Exclusive { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
            public int ContraAccountId { get; set; }
            public string Memo { get; set; }
            public bool HasAttachments { get; set; }
            public bool Editable { get; set; }
            public bool Locked { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
        }


        #endregion

        #region Methods / API Calls

        public List<CompaniesResult.Result> GetCompanies()
        {
            List<CompaniesResult.Result> _companies = new List<CompaniesResult.Result>();

            var companiesResult = Get<CompaniesResult>("Company/Get", "", 0);
            if (companiesResult.Results.Length > 0)
            {
                _companies.AddRange(companiesResult.Results);
                int count = 1;

                while (companiesResult.Results.Length == 100)
                {
                    companiesResult = Get<CompaniesResult>("Company/Get", "", count * 100);
                    _companies.AddRange(companiesResult.Results);
                    count++;
                }
            }
            return _companies;
        }

        public List<AccountCategory> GetAccountCategories(int companyID)
        {
            List<AccountCategory> _accountCategories = new List<AccountCategory>();

            var accountCategoryResult = Get<AccountCategoryResult>("AccountCategory/Get", $"&CompanyID={companyID}", 0);
            if (accountCategoryResult.Results.Length > 0)
            {
                _accountCategories.AddRange(accountCategoryResult.Results);
                int count = 1;

                while (accountCategoryResult.Results.Length == 100)
                {
                    accountCategoryResult = Get<AccountCategoryResult>("AccountCategory/Get", $"&CompanyID={companyID}", count * 100);
                    _accountCategories.AddRange(accountCategoryResult.Results);
                    count++;
                }
            }
            return _accountCategories;
        }

        public List<AccountTaxTypesResult.Result> GetAccountTaxTypes(int companyID)
        {
            List<AccountTaxTypesResult.Result> _AccountTaxTypes = new List<AccountTaxTypesResult.Result>();

            var AccountTaxTypesResult = Get<AccountTaxTypesResult>("TaxType/Get", $"&CompanyID={companyID}", 0);
            if (AccountTaxTypesResult.Results.Length > 0)
            {
                _AccountTaxTypes.AddRange(AccountTaxTypesResult.Results);
                int count = 1;

                while (AccountTaxTypesResult.Results.Length == 100)
                {
                    AccountTaxTypesResult = Get<AccountTaxTypesResult>("TaxType/Get", $"&CompanyID={companyID}", count * 100);
                    _AccountTaxTypes.AddRange(AccountTaxTypesResult.Results);
                    count++;
                }
            }
            return _AccountTaxTypes;
        }

        public List<GetChartOfAccountsResult.Result> GetAccounts(int companyID)
        {
            List<GetChartOfAccountsResult.Result> _Accounts = new List<GetChartOfAccountsResult.Result>();

            var AccountsResult = Get<GetChartOfAccountsResult>("Account/GetChartOfAccounts", $"&CompanyID={companyID}", 0);
            if (AccountsResult.Results.Length > 0)
            {
                _Accounts.AddRange(AccountsResult.Results);
                int count = 1;

                while (AccountsResult.Results.Length == 100)
                {
                    AccountsResult = Get<GetChartOfAccountsResult>("Account/GetChartOfAccounts", $"&CompanyID={companyID}", count * 100);
                    _Accounts.AddRange(AccountsResult.Results);
                    count++;
                }
            }
            return _Accounts;
        }

        public List<DetailedLedgerTransactionsResult.Result> GetDetailedLedgerTransactions(int companyID, DateTime fromDate, DateTime toDate, string fromAccount = "", string toAccount = "")
        {
            DetailedLedgerTransactionsRequest detailedLedgerTransactionsRequest = new DetailedLedgerTransactionsRequest()
            {
                Active = true,
                ConsolidateAdditionalCost = true,
                FromAccount = fromAccount,
                AccountCategorieIds = new List<object>().ToArray(),
                FromDate = fromDate.ToString("yyyy-MM-yy"),
                Inactive = true,
                IncludeNoTax = true,
                ToAccount = toAccount,
                ToDate = toDate.ToString("yyyy-MM-yy"),
                TransactionTypes = new List<object>().ToArray(),
            };

            List<DetailedLedgerTransactionsResult.Result> _DetailedLedgerTransactions = new List<DetailedLedgerTransactionsResult.Result>();

            var DetailedLedgerTransactionsResult = Post<DetailedLedgerTransactionsResult, DetailedLedgerTransactionsRequest>("DetailedLedgerTransaction/Get", detailedLedgerTransactionsRequest, $"&CompanyID={companyID}", 0);
            if (DetailedLedgerTransactionsResult.Results.Length > 0)
            {
                _DetailedLedgerTransactions.AddRange(DetailedLedgerTransactionsResult.Results);
                int count = 1;

                while (DetailedLedgerTransactionsResult.Results.Length == 100)
                {
                    DetailedLedgerTransactionsResult = Post<DetailedLedgerTransactionsResult, DetailedLedgerTransactionsRequest>("DetailedLedgerTransaction/Get", detailedLedgerTransactionsRequest, $"&CompanyID={companyID}", count * 100);
                    _DetailedLedgerTransactions.AddRange(DetailedLedgerTransactionsResult.Results);
                    count++;
                }
            }
            return _DetailedLedgerTransactions;
        }

        public SaveJournalEntryResponse SaveJournalEntry(int companyID, int AccountId, int TaxTypeId, decimal TaxTypePerc, int ContraAccountId, decimal Amount, DateTime Date, string Description, string Reference, int AccountingChecklistID)
        {
            SaveJournalEntryRequest saveJournalEntryRequest = new SaveJournalEntryRequest()
            {
                Exclusive = Amount,
                Tax = Amount * TaxTypePerc,
                Total = Amount * (1 + TaxTypePerc),
                AccountId = AccountId,
                ContraAccountId = ContraAccountId,
                Date = Date.ToString("yyyy-MM-dd"),
                Description = Description,
                Reference = Reference,
                Credit = Amount < 0 ? Amount : 0,
                Debit = Amount >= 0 ? Amount : 0,
                TaxTypeId = TaxTypeId,
                Effect = Amount < 0 ? 2 : 1,
                Memo = "",
                Editable = false,
                HasAttachments = false,
                Locked = false,
            };

            if (saveJournalEntryRequest.Exclusive < 0)
                saveJournalEntryRequest.Exclusive = saveJournalEntryRequest.Exclusive * -1.0m;
            if (saveJournalEntryRequest.Tax < 0)
                saveJournalEntryRequest.Tax = saveJournalEntryRequest.Tax * -1.0m;
            if (saveJournalEntryRequest.Total < 0)
                saveJournalEntryRequest.Total = saveJournalEntryRequest.Total * -1.0m;
            if (saveJournalEntryRequest.Credit < 0)
                saveJournalEntryRequest.Credit = saveJournalEntryRequest.Credit * -1.0m;
            if (saveJournalEntryRequest.Debit < 0)
                saveJournalEntryRequest.Debit = saveJournalEntryRequest.Debit * -1.0m;


            var db = new MyVoltageDbContext(_options);
            SageAccounting_JournalLog sageAccounting_JournalLog = new SageAccounting_JournalLog()
            {
                Request = JsonConvert.SerializeObject(saveJournalEntryRequest),
                Response = "",
                AccountingChecklistID = AccountingChecklistID,
                Amount = null,
                DateCreated = DateTime.Now,
                ResponseCode = null,
                SageID = null,
            };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/JournalEntry/Save?apikey={token}{$"&CompanyID={companyID}"}");
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}", password));

            HttpWebResponse myHttpWebResponse;

            string json = JsonConvert.SerializeObject(saveJournalEntryRequest);
            byte[] byteArray = Encoding.ASCII.GetBytes(json);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                sageAccounting_JournalLog.ResponseCode = 500;
                sageAccounting_JournalLog.Response = ex.ToString();
                db.Add(sageAccounting_JournalLog);
                db.SaveChanges();

                return null;
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }
            sageAccounting_JournalLog.ResponseCode = (int)myHttpWebResponse.StatusCode;
            sageAccounting_JournalLog.Response = responseText;

            myHttpWebResponse.Close();

            SaveJournalEntryResponse response = null;

            try
            {
                response = JsonConvert.DeserializeObject<SaveJournalEntryResponse>(responseText);
            }
            catch
            {
                try
                {
                    // remove []
                    responseText = responseText.Remove(responseText.IndexOf('['), 1);
                    responseText = responseText.Remove(responseText.LastIndexOf(']'), 1);
                    response = JsonConvert.DeserializeObject<SaveJournalEntryResponse>(responseText);
                }
                catch (Exception ex)
                {
                    sageAccounting_JournalLog.ResponseCode = 500;
                    sageAccounting_JournalLog.Response = ex.ToString();
                    db.Add(sageAccounting_JournalLog);
                    db.SaveChanges();

                    return null;
                }

            }

            if (response != null)
            {
                sageAccounting_JournalLog.Amount = response.Exclusive;
                sageAccounting_JournalLog.SageID = response.ID;
            }
            db.Add(sageAccounting_JournalLog);
            db.SaveChanges();

            return response;
        }

        public void DeleteJournal(long journalID, int companyID)
        {
            var response = DELETE<object>($"JournalEntry/Delete/{journalID}", companyID);
        }

        #endregion

    }
}
