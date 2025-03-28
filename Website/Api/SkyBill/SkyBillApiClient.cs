using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.Caching.Memory;
using MoreLinq;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Services;
using MyVoltage.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{

    public class Tarrifs
    {
        public string odatacontext { get; set; }
        public Tarrif[] value { get; set; }
        public class Tarrif
        {
            public string odataetag { get; set; }
            public string Resource_No { get; set; }
            public string Sales_Type { get; set; }
            public string Sales_Code { get; set; }
            public DateTime Starting_Date { get; set; }
            public decimal Quantity_From { get; set; }
            public string Resource_Name { get; set; }
            public float Unit_Price { get; set; }
            public int Unit_Price_2 { get; set; }
            public int Unit_Cost { get; set; }
            public int Profit { get; set; }
            public bool Flat_Rate { get; set; }
            public string ETag { get; set; }
        }
    }



    public class SkyBillApiClient : ApiClient
    {
        private string _company;
        private readonly IMemoryCache _cache;
        private readonly bool _useAzure;
        public string _Username = "webservice";
        public string _Password = "MyVoltage_001";
        public string _BaseURL = $"https://20.87.10.102";
        public string _BaseURL_EU
        {
            get
            {
                return $"{_BaseURL}:30148/SBUBNZ/";
            }
        }
        public string _BaseURL_Azure
        {
            get
            {
                return $"{_BaseURL}:51348/SBNZBC/";
            }
        }

        public SkyBillApiClient(string company, IMemoryCache cache, bool useAzure = false)
        {
            _company = company;
            _cache = cache;
            _useAzure = useAzure;
        }

        public T Get<T>(string method, string filter, bool useFilter = true, int offset = 0, string company = "", string fullUrl = "")
        {
            string username = "webservice";
            string password = "MyVoltage_001";

            string filterStr = "";

            if (useFilter)
            {
                filterStr = $"&$filter={filter}";
            }

            string offsetStr = "";

            if (offset > 0)
            {
                offsetStr = $"&$skip={offset}";
            }

            if (String.IsNullOrEmpty(company))
                company = _company;

            DateTime startTime = DateTime.Now;

            string url = $"https://20.87.10.102:30148/SBUBNZ/ODataV4/Company('{company}')/{method}?tenant=1f998066-df97-4de7-875d-0179278815b1{filterStr}{offsetStr}"; // Original V1
            //string url = $"https://20.87.10.102:51348/SBNZBC/ODataV4/Company('{company}')/{method}?tenant=1f998066-df97-4de7-875d-0179278815b1{filterStr}{offsetStr}"; // Azure
            // https://mvbc.skybill.eu/SBNZBC/ODataV4/
            if (_useAzure)
            {
                url = $"https://20.87.10.102:51348/SBNZBC/ODataV4/Company('{company}')/{method}?tenant=1f998066-df97-4de7-875d-0179278815b1{filterStr}{offsetStr}";
            }

            if (!String.IsNullOrEmpty(fullUrl))
            {
                url = fullUrl;
                if (_useAzure)
                {
                    url = url.Replace("skybill.eu:30148/SBUBNZ", "skybill.eu:51348/SBNZBC");
                }
            }



            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add("ContentType", "application/json");
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", base.GetAuthHeader(username, password));

            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var result = client.SendAsync(message).Result;

            //if (result.StatusCode != HttpStatusCode.OK)
            //{
            //    Log.Error(url + "\r\n" + result.ToString());
            //}

            var stringResponse = result.Content.ReadAsStringAsync().Result;

            var root = JsonConvert.DeserializeObject<T>(stringResponse);

            var totalDuration = DateTime.Now - startTime;

            if (_useAzure)
            {
                Console.WriteLine($"AZURE - {totalDuration.TotalMilliseconds:N} - {url}");
            }
            else
                Console.WriteLine($"EU - {totalDuration.TotalMilliseconds:N} - {url}");


            return root;
        }

        public List<MeterListItem> GetMetersByCompany2()
        {
            var allLedgerEntries = new List<MeterListItem>();

            var root = Get<MeterList>("MeterList", "", false);

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<MeterList>("MeterList", "", false, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<MeterListItem>();

        }

        public List<Tarrifs.Tarrif> GetTarrifsForCompany()
        {
            var allLedgerEntries = new List<Tarrifs.Tarrif>();

            var root = Get<Tarrifs>("Tariffs", "", false);

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<Tarrifs>("Tariffs", "", false, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<Tarrifs.Tarrif>();

        }

        public Tuple<Tarrifs.Tarrif, DateTime?> GetTarrif(string resource_No, DateTime dateToCheck, List<Tarrifs.Tarrif> tarrifs = null)
        {
            Tuple<Tarrifs.Tarrif, DateTime?> tuple = new Tuple<Tarrifs.Tarrif, DateTime?>(null, null);
            if (tarrifs == null)
                tarrifs = GetTarrifsForCompany();

            var currentItemTarrifs = tarrifs.Where(p => p.Resource_No == resource_No).ToList();
            var currentItemTarrifsDates = currentItemTarrifs.Select(p => p.Starting_Date).Distinct().OrderBy(p => p).ToList();

            for (int i = 0; i < currentItemTarrifsDates.Count; i++)
            {
                var currentTarrifStartDate = currentItemTarrifsDates[i];
                DateTime? endDate = null;
                try
                {
                    endDate = currentItemTarrifsDates[i + 1].AddDays(-1);
                }
                catch { }

                if (currentTarrifStartDate <= dateToCheck)
                {
                    var t = tarrifs.Where(p => p.Resource_No == resource_No && p.Starting_Date == currentTarrifStartDate).FirstOrDefault();
                    if (endDate.HasValue && endDate.Value >= dateToCheck)
                    {
                        tuple = new Tuple<Tarrifs.Tarrif, DateTime?>(t, endDate);
                        break;
                    }
                    tuple = new Tuple<Tarrifs.Tarrif, DateTime?>(t, endDate);
                }
            }


            return tuple;
        }

        public Tuple<Tarrifs.Tarrif, DateTime?> GetTarrifFromName(string resource_Name, DateTime dateToCheck, List<Tarrifs.Tarrif> tarrifs = null)
        {
            Tuple<Tarrifs.Tarrif, DateTime?> tuple = new Tuple<Tarrifs.Tarrif, DateTime?>(null, null);
            if (tarrifs == null)
                tarrifs = GetTarrifsForCompany();

            var currentItemTarrif = tarrifs.Where(p => p.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)).FirstOrDefault();

            if (currentItemTarrif != null)
                return GetTarrif(currentItemTarrif.Resource_No, dateToCheck, tarrifs);

            return tuple;
        }

        public List<GetAllCustomersRootObject.Value> GetAllCustomers()
        {
            var root = Get<GetAllCustomersRootObject>("Customers", "", useFilter: false);

            var allLedgerEntries = new List<GetAllCustomersRootObject.Value>();

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<GetAllCustomersRootObject>("Customers", "", false, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<GetAllCustomersRootObject.Value>();

        }

        public Customer GetCustomerByMeterNumber(string meterNumber, string company = "")
        {
            var root = Get<CustomerRoot>("CustomerMeters", $"Serial_No eq '{meterNumber}'", true, 0, company);

            if (root.value != null && root.value.Length > 0)
                return root.value[0];

            return null;
        }

        public Customer GetCustomerByMeterNumberInAllCompanies(string meterNumber, List<Company> companies)
        {
            foreach (Company c in companies)
            {
                Customer customer = GetCustomerByMeterNumber(meterNumber, c.Name);

                if (customer != null)
                {
                    customer.company = c;
                    return customer;
                }
            }

            return null;
        }

        public CustomerDetails GetCustomerDetailsByCustomerNo(string cutomerNumber, string company)
        {
            var key = company + "Customer_" + cutomerNumber;

            CustomerDetails customerDetails = new CustomerDetails();

            if (!_cache.TryGetValue<CustomerDetails>(key, out customerDetails))
            {
                var root = Get<CustomerDetailsRoot>("Customers", $"No eq '{cutomerNumber}'", true, 0, company);

                if (root.value != null && root.value.Length > 0)
                    return root.value[0];

                return null;
            }
            else
            {
                return customerDetails;
            }
        }

        public List<Customer> GetMetersByCustomer(string customerNo)
        {
            var root = Get<CustomerRoot>("CustomerMeters", $"Customer_No eq '{customerNo}'");

            if (root.value != null)
            {
                return root.value.DistinctBy(x => x.Serial_No).ToList();
            }

            return new List<Customer>();
        }

        public List<Customer> GetMetersByCompany()
        {
            var root = Get<CustomerRoot>("CustomerMeters", "", false);

            if (root.value != null)
            {
                return root.value.DistinctBy(x => x.Serial_No).ToList();
            }

            return new List<Customer>();

        }
        public List<Customer> GetAllCustomerMeters()
        {
            List<Customer> customerList = new List<Customer>();

            var key = _company + "CustomerMeters";

            if (!_cache.TryGetValue<List<Customer>>(key, out customerList))
            {
                var root = Get<CustomerRoot>("CustomerMeters", "", false);

                if (root != null && root.value != null)
                {
                    List<Customer> tmpCustomerList = root.value.ToList();

                    while (!string.IsNullOrEmpty(root.odatanextLink))
                    {
                        root = Get<CustomerRoot>("CustomerMeters", "", false, 0, "", root.odatanextLink);

                        tmpCustomerList.AddRange(root.value.ToList());
                    }

                    customerList = tmpCustomerList;

                    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(20);
                    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                    _cache.Set<List<Customer>>(key, customerList, cacheExpirationOptions);
                }
                return customerList;
            }
            else
            {
                return customerList;
            }
        }


        public List<Ledger> GetLedgerEntriesByCustomer(string customerNo)
        {
            var key = _company + "CustomerLedgerEntries_" + customerNo;

            List<Ledger> customerList = new List<Ledger>();

            if (!_cache.TryGetValue<List<Ledger>>(key, out customerList))
            {
                var root = Get<LedgerRoot>("CustomerLedgerEntries", $"Customer_No eq '{customerNo}'");
                customerList = new List<Ledger>();

                if (root != null && root.value != null && root.value.Length > 0)
                {
                    List<Ledger> tmpCustomerList = root.value.ToList();
                    customerList.AddRange(tmpCustomerList);

                    int count = 1;

                    while (tmpCustomerList.Count == 1000)
                    {
                        root = Get<LedgerRoot>("CustomerLedgerEntries", $"Customer_No eq '{customerNo}'", true, count * 1000);

                        tmpCustomerList = root.value.ToList();

                        customerList.AddRange(tmpCustomerList);

                        count++;
                    }
                    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(20);
                    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                    _cache.Set<List<Ledger>>(key, customerList, cacheExpirationOptions);
                }
                return customerList;
            }
            else
            {
                return customerList;
            }
        }

        public List<LedgerLineItem> GetSalesInvoiceLinesByCustomer(string customerNo)
        {
            var allLedgerEntries = new List<LedgerLineItem>();

            var root = Get<LedgerLineItemRoot>("SalesInvoiceLines", $"Bill_to_Customer_No eq '{customerNo}'");

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<LedgerLineItemRoot>("SalesInvoiceLines", $"Bill_to_Customer_No eq '{customerNo}'", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<LedgerLineItem>();

        }

        public Customer GetCustomer(string customerNo)
        {
            var root = Get<CustomerRoot>("CustomerMeters", $"Customer_No eq '{customerNo}'");

            if (root.value != null)
            {
                return root.value.ToList().FirstOrDefault();
            }

            return new Customer();
        }

        public List<LedgerLineItem> GetSalesInvoiceLinesByCustomer(string customerNo, DateTime startDate, DateTime endDate)
        {
            var allLedgerEntries = new List<LedgerLineItem>();

            startDate = startDate.AddDays(-2);
            endDate = endDate.AddDays(2);

            var root = Get<LedgerLineItemRoot>("SalesInvoiceLines", $"Bill_to_Customer_No eq '{customerNo}' and Posting_Date gt {startDate:yyyy-MM-dd} and Posting_Date lt {endDate:yyyy-MM-dd}");

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<LedgerLineItemRoot>("SalesInvoiceLines", $"Bill_to_Customer_No eq '{customerNo}' and Posting_Date gt {startDate:yyyy-MM-dd} and Posting_Date lt {endDate:yyyy-MM-dd}", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries.Where(p => p.Posting_Date >= startDate.Date && p.Posting_Date.Date <= endDate.Date).ToList();
            }

            return new List<LedgerLineItem>();

        }

        public List<CustomersUtilityReadings.Value> GetCustomersUtilityReadings(string customerNo, DateTime startDate, DateTime endDate)
        {
            var allLedgerEntries = new List<CustomersUtilityReadings.Value>();

            var root = Get<CustomersUtilityReadings>("CustomersUtilityReadings", $"Customer_No eq '{customerNo}' and Current_Reading_Date gt {startDate:yyyy-MM-dd} and Current_Reading_Date lt {endDate:yyyy-MM-dd}");

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<CustomersUtilityReadings>("CustomersUtilityReadings", $"Customer_No eq '{customerNo}' and Current_Reading_Date gt {startDate:yyyy-MM-dd} and Current_Reading_Date lt {endDate:yyyy-MM-dd}", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<CustomersUtilityReadings.Value>();
        }

        public List<ServiceUsageLedgerEntries.Value> GetServiceUsageLedgerEntries(string customerNo, DateTime startDate, DateTime endDate)
        {
            var allLedgerEntries = new List<ServiceUsageLedgerEntries.Value>();

            var root = Get<ServiceUsageLedgerEntries>("ServiceUsageLedgerEntries", $"Customer_No eq '{customerNo}' and Current_Reading_Date gt {startDate:yyyy-MM-dd} and Current_Reading_Date lt {endDate:yyyy-MM-dd}");

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<ServiceUsageLedgerEntries>("ServiceUsageLedgerEntries", $"Customer_No eq '{customerNo}' and Current_Reading_Date gt {startDate:yyyy-MM-dd} and Current_Reading_Date lt {endDate:yyyy-MM-dd}", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<ServiceUsageLedgerEntries.Value>();
        }

        public List<CustomersUtilities.Value> GetCustomersUtilities()
        {
            var allLedgerEntries = new List<CustomersUtilities.Value>();

            var root = Get<CustomersUtilities>("CustomersUtilities", "", false);

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<CustomersUtilities>("CustomersUtilities", "", false, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<CustomersUtilities.Value>();
        }
        public List<Ledger> GetLedgerEntriesByCustomer(string customerNo, DateTime startDate, DateTime endDate)
        {
            var root = Get<LedgerRoot>("CustomerLedgerEntries", $"Customer_No eq '{customerNo}' and Posting_Date gt {startDate:yyyy-MM-dd} and Posting_Date lt {endDate:yyyy-MM-dd}");

            var allLedgerEntries = new List<Ledger>();

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<LedgerRoot>("CustomerLedgerEntries", $"Customer_No eq '{customerNo}' and Posting_Date gt {startDate:yyyy-MM-dd} and Posting_Date lt {endDate:yyyy-MM-dd}", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<Ledger>();

        }


        public Stream GetTaxInvoice(Data.Customer localCustomer, Data.Company company, DateTime invoiceMonth, string templateFileName, string logoPATH)
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = GetTenantConsumptionInvoice(localCustomer.CustomerNumber, company.Name, invoiceMonth);

            var customer = GetCustomer(localCustomer.CustomerNumber);

            var customerLedgerEntries = GetLedgerEntriesByCustomer(localCustomer.CustomerNumber, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (tenantConsumptionStatementItems.Count == 0)
                return null;

            CExcel.TaxInvoiceTemplate taxInvoiceTemplate = GetTaxInvoiceTaxInvoiceTemplate(localCustomer, company, invoiceMonth);

            return CExcel.GenerateTaxInvoice(taxInvoiceTemplate, templateFileName, fillCells: true, logoPATH);
        }

        public CExcel.TaxInvoiceTemplate GetTaxInvoiceTaxInvoiceTemplate(Data.Customer localCustomer, Data.Company company, DateTime invoiceMonth)
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = GetTenantConsumptionInvoice(localCustomer.CustomerNumber, company.Name, invoiceMonth);

            var customer = GetCustomer(localCustomer.CustomerNumber);

            var customerLedgerEntries = GetLedgerEntriesByCustomer(localCustomer.CustomerNumber, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (tenantConsumptionStatementItems.Count == 0)
                return null;

            #region Save Invoice Template

            CExcel.TaxInvoiceTemplate taxInvoiceTemplate = new CExcel.TaxInvoiceTemplate()
            {
                CustomerNo = localCustomer.CustomerNumber,
                AccountName = customer != null ? customer.Customer_Name : "DELETED",
                Address1 = !string.IsNullOrEmpty(localCustomer?.RecipientAddress) ? localCustomer.RecipientAddress : localCustomer.StreetAddress,
                Address2 = localCustomer.Suburb,
                Address3 = localCustomer.TownOrCity,
                Address4 = localCustomer.Province,
                Address5 = localCustomer.PostalCode.ToString(),
                BalanceBroughForward = 0,
                BalanceDue = 0,
                EndDate = tenantConsumptionStatementItems.Select(p => p.EndDate).Max().ToString("dd MMMM yyyy"),
                InvoiceNo = $"{localCustomer.CustomerNumber}_{(tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("yyyy_MM"))}",
                PaymentDueDate = DateTime.Now.ToString("dd MMMM yyyy"),
                PaymentsReceived = 0,
                StartDate = tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("dd MMMM yyyy"),
                SupplyAddress = "",
                TaxDate = DateTime.Now.ToString("dd MMMM yyyy"),
                VatNo = "",
                VATOnCurrentCharges = 0.15m,
                Items = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem>(),
                LedgerItems = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem>(),
                ClosingBalance = 0,
                OpeningBalance = 0,
                SupplierVATNumber = company.SupplierVATNumber,
                SupplierName = company.SupplierName,
                SupplierAddress = company.SupplierAddress,
                NetcashBankAccountNo = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo,
                NetcashBankAccountType = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType,
                NetcashBankBranchCode = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode,
                NetcashBankName = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName,
                RecipientAddress = localCustomer.RecipientAddress,
                RecipientName = localCustomer.RecipientName,
                RecipientReferenceNumber = localCustomer.RecipientReferenceNumber,
                RecipientVATNumber = localCustomer.RecipientVATNumber,
                SupplierPhone = company.SupplierPhone,
                SupplierPostal = company.SupplierPostal,
                SupplierURL = company.SupplierURL,
            };

            foreach (var tenantitem in tenantConsumptionStatementItems)
            {
                taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                {
                    Description = tenantitem.Description,
                    ClosingReading = tenantitem.ClosingReading,
                    MeterSerial = tenantitem.MeterSerial,
                    OpeningReading = tenantitem.MeterSerial.ToUpper().Contains("-KVA") ? 0 : (tenantitem.OpeningReading > tenantitem.ClosingReading ? 0 : tenantitem.OpeningReading),
                    TotalExVAT = tenantitem.TotalExVAT,
                    ItemResourceType = tenantitem.ItemResourceType,
                });
            }

            foreach (var ledger in customerLedgerEntries)
            {
                if (ledger.Description.ToUpper().Contains("FEE")
                    || ledger.Document_Type.ToUpper().Contains("PAYMENT"))
                {
                    taxInvoiceTemplate.LedgerItems.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem()
                    {
                        Date = ledger.Posting_Date,
                        Description = $"{ledger.Description} - {ledger.Document_No}",
                        TotalExVAT = Convert.ToDecimal(ledger.Amount * -1)
                    });
                }
            }

            taxInvoiceTemplate.PaymentsReceived = taxInvoiceTemplate.LedgerItems.Select(p => p.TotalExVAT).Sum();

            return taxInvoiceTemplate;

            #endregion
        }

        public Stream GetTenantConsumptionInvoice(string customerNo, string companyName, DateTime invoiceMonth, string templateFileName, Data.Company company, Data.Customer localCustomer, bool includeBalances = false, string logoPATH = "")
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth);

            var customer = GetCustomer(customerNo);

            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (tenantConsumptionStatementItems.Count == 0)
                return null;

            CExcel.TaxInvoiceTemplate taxInvoiceTemplate = GetTenantConsumptionInvoiceTaxInvoiceTemplate(customerNo, companyName, invoiceMonth, company, localCustomer, includeBalances);

            return CExcel.GenerateTaxInvoice(taxInvoiceTemplate, templateFileName, logoPATH: logoPATH);
        }

        public CExcel.TaxInvoiceTemplate GetTenantConsumptionInvoiceTaxInvoiceTemplate(string customerNo, string companyName, DateTime invoiceMonth, Data.Company company, Data.Customer localCustomer, bool includeBalances = false)
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth);

            var customer = GetCustomer(customerNo);

            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (tenantConsumptionStatementItems.Count == 0)
                return null;

            #region Save Invoice Template

            CExcel.TaxInvoiceTemplate taxInvoiceTemplate = new CExcel.TaxInvoiceTemplate()
            {
                CustomerNo = customerNo,
                AccountName = customer != null ? customer.Customer_Name : "DELETED",
                Address1 = !string.IsNullOrEmpty(localCustomer?.RecipientAddress) ? localCustomer.RecipientAddress : localCustomer.StreetAddress,
                Address2 = localCustomer.Suburb,
                Address3 = localCustomer.TownOrCity,
                Address4 = localCustomer.Province,
                Address5 = localCustomer.PostalCode.ToString(),
                BalanceBroughForward = 0,
                BalanceDue = 0,
                EndDate = tenantConsumptionStatementItems.Select(p => p.EndDate).Max().ToString("dd MMMM yyyy"),
                InvoiceNo = $"{customerNo}_{(tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("yyyy_MM"))}",
                PaymentDueDate = DateTime.Now.ToString("dd MMMM yyyy"),
                PaymentsReceived = 0,
                StartDate = tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("dd MMMM yyyy"),
                SupplyAddress = "",
                TaxDate = DateTime.Now.ToString("dd MMMM yyyy"),
                VatNo = "",
                VATOnCurrentCharges = 0.15m,
                Items = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem>(),
                LedgerItems = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem>(),
                ClosingBalance = 0,
                OpeningBalance = 0,
                NetcashBankAccountNo = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo,
                NetcashBankAccountType = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType,
                NetcashBankBranchCode = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode,
                NetcashBankName = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName,
                RecipientAddress = localCustomer.RecipientAddress,
                RecipientName = localCustomer.RecipientName,
                RecipientReferenceNumber = localCustomer.RecipientReferenceNumber,
                RecipientVATNumber = localCustomer.RecipientVATNumber,
                SupplierAddress = company.SupplierAddress,
                SupplierName = company.SupplierName,
                SupplierVATNumber = company.SupplierVATNumber,
                SupplierPhone = company.SupplierPhone,
                SupplierPostal = company.SupplierPostal,
                SupplierURL = company.SupplierURL,
            };

            if (includeBalances)
            {
                var allCustomerLedgers = GetLedgerEntriesByCustomer(customerNo);
                try
                {
                    taxInvoiceTemplate.OpeningBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum()) * -1.0m;
                }
                catch { }
                try
                {
                    taxInvoiceTemplate.ClosingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum()) * -1.0m;
                }
                catch { }
            }

            foreach (var tenantitem in tenantConsumptionStatementItems)
            {
                taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                {
                    Description = tenantitem.Description,
                    ClosingReading = tenantitem.ClosingReading,
                    MeterSerial = tenantitem.MeterSerial,
                    OpeningReading = tenantitem.MeterSerial.ToUpper().Contains("-KVA") ? 0 : (tenantitem.OpeningReading > tenantitem.ClosingReading ? 0 : tenantitem.OpeningReading),
                    TotalExVAT = tenantitem.TotalExVAT,
                    ItemResourceType = tenantitem.ItemResourceType,
                });
            }

            foreach (var ledger in customerLedgerEntries)
            {
                if (ledger.Description.ToUpper().Contains("FEE")
                    || ledger.Document_Type.ToUpper().Contains("CREDIT MEMO")
                    || ledger.Document_Type.ToUpper().Contains("PAYMENT"))
                {
                    taxInvoiceTemplate.LedgerItems.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem()
                    {
                        Date = ledger.Posting_Date,
                        Description = $"{ledger.Description} - {ledger.Document_No}",
                        TotalExVAT = Convert.ToDecimal(ledger.Amount * -1)
                    });
                }
                //if (ledger.Description.ToUpper().Contains("INVOICE"))
                //{
                //    taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                //    {
                //        Description = ledger.Description,
                //        ClosingReading = 0,
                //        MeterSerial = "",
                //        OpeningReading = 0,
                //        TotalExVAT = Convert.ToDecimal(ledger.Amount)
                //    });
                //}
            }

            taxInvoiceTemplate.PaymentsReceived = taxInvoiceTemplate.LedgerItems.Select(p => p.TotalExVAT).Sum();

            return taxInvoiceTemplate;

            #endregion
        }

        public List<CExcel.TaxInvoiceTemplate> GetTenantConsumptionInvoiceTaxInvoiceTemplateGroupedByMonth(string customerNo, string companyName, DateTime fromDate, DateTime toDate, Data.Company company, Data.Customer localCustomer)
        {
            List<CExcel.TaxInvoiceTemplate> taxInvoiceTemplates = new List<CExcel.TaxInvoiceTemplate>();

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItemsAll = GetTenantConsumptionInvoiceGroupedByMonth(customerNo, companyName, fromDate, toDate);

            if (tenantConsumptionStatementItemsAll.Count == 0)
                return null;

            DateTime endOfToMonth = new DateTime(toDate.Year, toDate.Month, DateTime.DaysInMonth(toDate.Year, toDate.Month));

            var customer = GetCustomer(customerNo);
            var customerLedgerEntriesAll = GetLedgerEntriesByCustomer(customerNo, fromDate.AddDays(-1), endOfToMonth.AddDays(1));
            var allCustomerLedgers = GetLedgerEntriesByCustomer(customerNo);

            DateTime invoiceMonth = new DateTime(fromDate.Year, fromDate.Month, 1);
            while (invoiceMonth <= toDate)
            {
                List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = tenantConsumptionStatementItemsAll.Where(p => p.Month == invoiceMonth).ToList();

                if (tenantConsumptionStatementItems.Count != 0)
                {
                    DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
                    DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
                    DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);
                    var customerLedgerEntries = customerLedgerEntriesAll.Where(p => p.Posting_Date.Date >= startOfThisMonth.Date && p.Posting_Date.Date <= endOfThisMonth);

                    #region Save Invoice Template

                    CExcel.TaxInvoiceTemplate taxInvoiceTemplate = new CExcel.TaxInvoiceTemplate()
                    {
                        CustomerNo = customerNo,
                        AccountName = customer != null ? customer.Customer_Name : "DELETED",
                        Address1 = customer != null ? customer.Address : "DELETED",
                        Address2 = "",
                        Address3 = "",
                        BalanceBroughForward = 0,
                        BalanceDue = 0,
                        EndDate = tenantConsumptionStatementItems.Select(p => p.EndDate).Max().ToString("dd MMMM yyyy"),
                        InvoiceNo = $"{customerNo}_{(tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("yyyy_MM"))}",
                        PaymentDueDate = DateTime.Now.ToString("dd MMMM yyyy"),
                        PaymentsReceived = 0,
                        StartDate = tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("dd MMMM yyyy"),
                        SupplyAddress = "",
                        TaxDate = DateTime.Now.ToString("dd MMMM yyyy"),
                        VatNo = "",
                        VATOnCurrentCharges = 0.15m,
                        Items = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem>(),
                        LedgerItems = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem>(),
                        ClosingBalance = 0,
                        OpeningBalance = 0,
                        Month = invoiceMonth,
                        NetcashBankAccountNo = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo,
                        NetcashBankAccountType = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType,
                        NetcashBankBranchCode = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode,
                        NetcashBankName = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName,
                        RecipientAddress = localCustomer.RecipientAddress,
                        RecipientName = localCustomer.RecipientName,
                        RecipientReferenceNumber = localCustomer.RecipientReferenceNumber,
                        RecipientVATNumber = localCustomer.RecipientVATNumber,
                        SupplierAddress = company.SupplierAddress,
                        SupplierName = company.SupplierName,
                        SupplierVATNumber = company.SupplierVATNumber,
                        SupplierPhone = company.SupplierPhone,
                        SupplierPostal = company.SupplierPostal,
                        SupplierURL = company.SupplierURL,
                    };

                    try
                    {
                        taxInvoiceTemplate.OpeningBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum()) * -1.0m;
                    }
                    catch { }
                    try
                    {
                        taxInvoiceTemplate.ClosingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum()) * -1.0m;
                    }
                    catch { }

                    foreach (var tenantitem in tenantConsumptionStatementItems)
                    {
                        taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                        {
                            Description = tenantitem.Description,
                            ClosingReading = tenantitem.ClosingReading,
                            MeterSerial = tenantitem.MeterSerial,
                            OpeningReading = tenantitem.MeterSerial.ToUpper().Contains("-KVA") ? 0 : (tenantitem.OpeningReading > tenantitem.ClosingReading ? 0 : tenantitem.OpeningReading),
                            TotalExVAT = tenantitem.TotalExVAT,
                        });
                    }

                    foreach (var ledger in customerLedgerEntries)
                    {
                        if (ledger.Description.ToUpper().Contains("FEE")
                            || ledger.Document_Type.ToUpper().Contains("CREDIT MEMO")
                            || ledger.Document_Type.ToUpper().Contains("PAYMENT"))
                        {
                            taxInvoiceTemplate.LedgerItems.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem()
                            {
                                Date = ledger.Posting_Date,
                                Description = $"{ledger.Description} - {ledger.Document_No}",
                                TotalExVAT = Convert.ToDecimal(ledger.Amount * -1)
                            });
                        }
                        //if (ledger.Description.ToUpper().Contains("INVOICE"))
                        //{
                        //    taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                        //    {
                        //        Description = ledger.Description,
                        //        ClosingReading = 0,
                        //        MeterSerial = "",
                        //        OpeningReading = 0,
                        //        TotalExVAT = Convert.ToDecimal(ledger.Amount)
                        //    });
                        //}
                    }

                    taxInvoiceTemplate.PaymentsReceived = taxInvoiceTemplate.LedgerItems.Select(p => p.TotalExVAT).Sum();

                    taxInvoiceTemplates.Add(taxInvoiceTemplate);

                    #endregion
                }
                invoiceMonth = invoiceMonth.AddMonths(1);
            }
            return taxInvoiceTemplates;

        }

        public List<TenantConsumptionStatementItem> GetTenantConsumptionInvoice(string customerNo, string companyName, DateTime invoiceMonth)
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
            var customer = GetCustomer(customerNo);

            var customerMeters = GetMetersByCustomer(customerNo);
            var salesJournals = GetSalesInvoiceLinesByCustomer(customerNo, startOfThisMonth, endOfThisMonth);
            var customerUtilityReadings = GetCustomersUtilityReadings(customerNo, lastDayOfPreviousMonth.AddDays(-10), endOfThisMonth.AddDays(10));
            var serviceUsageLedgerEntries = GetServiceUsageLedgerEntries(customerNo, lastDayOfPreviousMonth.AddDays(-10), endOfThisMonth.AddDays(10));
            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (salesJournals.Count == 0 /*|| customerUtilityReadings.Count == 0*/)
                return tenantConsumptionStatementItems;

            List<string> resourceGroupNos = (from p in salesJournals
                                             select p.Resource_Group_No).Distinct().ToList();

            #region Meter Charges

            var serials = (from p in salesJournals
                           where !string.IsNullOrEmpty(p.Meter_Serial_No)
                           select new { p.Meter_Serial_No, p.Meter_No }).Distinct().ToList();

            if (customerMeters.Count > 0)
            {
                foreach (var customerMeter in customerMeters)
                {
                    var salesJournalsForThisMonth = (from p in salesJournals
                                                     where p.Posting_Date.Date >= startOfThisMonth.Date
                                                     && p.Posting_Date.Date <= endOfThisMonth.Date
                                                     && p.Meter_Serial_No == customerMeter.Serial_No
                                                     select p).ToList();

                    var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                select p.Description).Distinct();

                    foreach (var description in distinctDescriptions)
                    {
                        var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                        where p.Description == description
                                                        select p).ToList();
                        TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                        if (salesJournalsForThisDesc.Count > 0)
                        {
                            string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                      where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                      select p.Resource_Group_No).FirstOrDefault();

                            switch (resourceGroupNo)
                            {
                                default:
                                    resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                    break;
                                case "ELEC":
                                    resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                    break;
                                case "WATE":
                                    if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                        resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                    else
                                        resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                    break;
                            }
                        }

                        var totalTarrif = (from p in salesJournalsForThisMonth
                                           where p.Description == description
                                           select p.Amount).Sum();


                        // Last reading of previous month
                        var openingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date <= lastDayOfPreviousMonth.Date // Current_Reading_Date <= 2022-04-30
                                              && p.Current_reading > 0 // Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        float openingReadingValue = 0;

                        if (openingReading == null)
                        {
                            // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                            openingReadingValue = (from p in serviceUsageLedgerEntries
                                                   where p.Meter_No == customerMeter.No
                                                   && p.Customer_No == customerMeter.Customer_No
                                                   && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                   && p.Current_Reading_Date.Date >= startOfThisMonth.Date
                                                   && p.Current_reading > 0 // Current_reading > 0
                                                   orderby p.Current_Reading_Date ascending
                                                   select p.Previous_Reading).FirstOrDefault();
                        }
                        else
                            openingReadingValue = openingReading.Current_reading;

                        // Last reading of this month
                        var closingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        float closingReadingValue = 0;
                        if (closingReading != null)
                            closingReadingValue = closingReading.Current_reading;

                        TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                        {
                            CustomerNo = customerNo,
                            MeterNo = customerMeter.No,
                            MeterSerial = customerMeter.Serial_No,
                            Month = invoiceMonth,
                            Description = description,
                            TotalExVAT = totalTarrif,
                            OpeningReading = Convert.ToDecimal(openingReadingValue),
                            ClosingReading = Convert.ToDecimal(closingReadingValue),
                            ItemResourceType = resourceType
                        };

                        tenantConsumptionStatementItems.Add(statementItem);


                    }

                }
            }
            else if (serials.Count > 0)
            {
                foreach (var customerMeter in serials)
                {
                    var salesJournalsForThisMonth = (from p in salesJournals
                                                     where p.Posting_Date.Date >= startOfThisMonth.Date
                                                     && p.Posting_Date.Date <= endOfThisMonth.Date
                                                     && p.Meter_Serial_No == customerMeter.Meter_Serial_No
                                                     select p).ToList();

                    var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                select p.Description).Distinct();

                    foreach (var description in distinctDescriptions)
                    {
                        var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                        where p.Description == description
                                                        select p).ToList();
                        TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                        if (salesJournalsForThisDesc.Count > 0)
                        {
                            string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                      where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                      select p.Resource_Group_No).FirstOrDefault();

                            switch (resourceGroupNo)
                            {
                                default:
                                    resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                    break;
                                case "ELEC":
                                    resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                    break;
                                case "WATE":
                                    if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                        resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                    else
                                        resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                    break;
                            }
                        }

                        var totalTarrif = (from p in salesJournalsForThisMonth
                                           where p.Description == description
                                           select p.Amount).Sum();


                        // Last reading of previous month
                        var openingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.Meter_No
                                              && p.Current_Reading_Date.Date <= lastDayOfPreviousMonth.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        float openingReadingValue = 0;

                        if (openingReading == null)
                        {
                            // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                            openingReadingValue = (from p in serviceUsageLedgerEntries
                                                   where p.Meter_No == customerMeter.Meter_No
                                                   && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                   && p.Current_Reading_Date.Date >= startOfThisMonth.Date
                                                   && p.Current_reading > 0 // Current_reading > 0
                                                   orderby p.Current_Reading_Date ascending
                                                   select p.Previous_Reading).FirstOrDefault();
                        }

                        // Last reading of this month
                        var closingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.Meter_No
                                              && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                        {
                            CustomerNo = customerNo,
                            MeterNo = customerMeter.Meter_No,
                            MeterSerial = customerMeter.Meter_Serial_No,
                            Month = invoiceMonth,
                            Description = description,
                            TotalExVAT = totalTarrif,
                            OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                            ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                            ItemResourceType = resourceType
                        };

                        tenantConsumptionStatementItems.Add(statementItem);


                    }

                }
            }
            else
            {
                return tenantConsumptionStatementItems;
            }

            #endregion

            #region Fixed Charges

            var salesJournalsForThisMonthFC = (from p in salesJournals
                                               where p.Posting_Date.Year == invoiceMonth.Year
                                               && p.Posting_Date.Month == invoiceMonth.Month
                                               && string.IsNullOrEmpty(p.Meter_Serial_No)
                                               //&& p.Resource_Group_No == "FC"
                                               select p).ToList();

            var distinctDescriptionsFC = (from p in salesJournalsForThisMonthFC
                                          select p.Description).Distinct();

            foreach (var description in distinctDescriptionsFC)
            {
                var totalTarrif = (from p in salesJournalsForThisMonthFC
                                   where p.Description == description
                                   select p.Amount).Sum();

                // Last reading of previous month
                var openingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == lastDayOfPreviousMonth.Date
                                                  select p).FirstOrDefault();*/

                // Last reading of this month
                var closingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == endOfThisMonth.Date
                                                  select p).FirstOrDefault();*/

                TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                {
                    CustomerNo = customerNo,
                    MeterNo = "FC",
                    MeterSerial = "Other Charges",
                    Month = invoiceMonth,
                    Description = description,
                    TotalExVAT = totalTarrif,
                    OpeningReading = openingReading,
                    ClosingReading = closingReading
                };

                tenantConsumptionStatementItems.Add(statementItem);
            }


            #endregion

            return tenantConsumptionStatementItems;
        }

        public List<TenantConsumptionStatementItem> GetTenantConsumptionInvoiceGroupedByMonth(string customerNo, string companyName, DateTime fromDate, DateTime toDate)
        {
            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
            var customer = GetCustomer(customerNo);
            var customerMeters = GetMetersByCustomer(customerNo);

            DateTime invoiceMonth = new DateTime(fromDate.Year, fromDate.Month, 1);
            DateTime endOfToMonth = new DateTime(toDate.Year, toDate.Month, DateTime.DaysInMonth(toDate.Year, toDate.Month));

            var salesJournalsAll = GetSalesInvoiceLinesByCustomer(customerNo, fromDate, endOfToMonth);
            var customerUtilityReadingsAll = GetCustomersUtilityReadings(customerNo, fromDate.AddDays(-10), endOfToMonth.AddDays(10));
            var serviceUsageLedgerEntriesAll = GetServiceUsageLedgerEntries(customerNo, fromDate.AddDays(-10), endOfToMonth.AddDays(10));
            var customerLedgerEntriesAll = GetLedgerEntriesByCustomer(customerNo, fromDate.AddDays(-1), endOfToMonth.AddDays(1));

            if (salesJournalsAll.Count == 0 /*|| customerUtilityReadings.Count == 0*/)
                return tenantConsumptionStatementItems;

            while (invoiceMonth <= toDate)
            {
                DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
                DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
                DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

                var salesJournals = salesJournalsAll.Where(p => p.Posting_Date.Date >= startOfThisMonth.Date && p.Posting_Date.Date <= endOfThisMonth.Date);
                var customerUtilityReadings = customerUtilityReadingsAll.Where(p => p.Current_Reading_Date.Date >= lastDayOfPreviousMonth.AddDays(-10).Date && p.Current_Reading_Date.Date <= endOfThisMonth.AddDays(10));
                var serviceUsageLedgerEntries = serviceUsageLedgerEntriesAll.Where(p => p.Current_Reading_Date.Date >= lastDayOfPreviousMonth.AddDays(-10).Date && p.Current_Reading_Date.Date <= endOfThisMonth.AddDays(10));
                var customerLedgerEntries = customerLedgerEntriesAll.Where(p => p.Posting_Date.Date >= startOfThisMonth.AddDays(-1) && p.Posting_Date.Date <= endOfThisMonth.AddDays(1));

                List<string> resourceGroupNos = (from p in salesJournals
                                                 select p.Resource_Group_No).Distinct().ToList();

                #region Meter Charges

                var serials = (from p in salesJournals
                               where !string.IsNullOrEmpty(p.Meter_Serial_No)
                               select new { p.Meter_Serial_No, p.Meter_No }).Distinct().ToList();

                if (customerMeters.Count > 0)
                {
                    foreach (var customerMeter in customerMeters)
                    {
                        var salesJournalsForThisMonth = (from p in salesJournals
                                                         where p.Posting_Date.Date >= startOfThisMonth.Date
                                                         && p.Posting_Date.Date <= endOfThisMonth.Date
                                                         && p.Meter_Serial_No == customerMeter.Serial_No
                                                         select p).ToList();

                        var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                    select p.Description).Distinct();

                        foreach (var description in distinctDescriptions)
                        {
                            var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                            where p.Description == description
                                                            select p).ToList();
                            TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                            if (salesJournalsForThisDesc.Count > 0)
                            {
                                string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                          where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                          select p.Resource_Group_No).FirstOrDefault();

                                switch (resourceGroupNo)
                                {
                                    default:
                                        resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                        break;
                                    case "ELEC":
                                        resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                        break;
                                    case "WATE":
                                        if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                            resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                        else
                                            resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                        break;
                                }
                            }

                            var totalTarrif = (from p in salesJournalsForThisMonth
                                               where p.Description == description
                                               select p.Amount).Sum();


                            // Last reading of previous month
                            var openingReading = (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date <= lastDayOfPreviousMonth.Date // Current_Reading_Date <= 2022-04-30
                                                  && p.Current_reading > 0 // Current_reading > 0
                                                  orderby p.Current_Reading_Date descending
                                                  select p).FirstOrDefault();

                            float openingReadingValue = 0;

                            if (openingReading == null)
                            {
                                // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                                openingReadingValue = (from p in serviceUsageLedgerEntries
                                                       where p.Customer_No == customerMeter.Customer_No
                                                       && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                       && p.Current_Reading_Date.Date >= startOfThisMonth.Date
                                                       && p.Current_reading > 0 // Current_reading > 0
                                                       orderby p.Current_Reading_Date ascending
                                                       select p.Previous_Reading).FirstOrDefault();
                            }

                            // Last reading of this month
                            var closingReading = (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                  && p.Current_reading > 0
                                                  orderby p.Current_Reading_Date descending
                                                  select p).FirstOrDefault();

                            TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                            {
                                CustomerNo = customerNo,
                                MeterNo = customerMeter.No,
                                MeterSerial = customerMeter.Serial_No,
                                Month = invoiceMonth,
                                Description = description,
                                TotalExVAT = totalTarrif,
                                OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                                ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                ItemResourceType = resourceType
                            };

                            tenantConsumptionStatementItems.Add(statementItem);


                        }

                    }
                }
                else if (serials.Count > 0)
                {
                    foreach (var customerMeter in serials)
                    {
                        var salesJournalsForThisMonth = (from p in salesJournals
                                                         where p.Posting_Date.Date >= startOfThisMonth.Date
                                                         && p.Posting_Date.Date <= endOfThisMonth.Date
                                                         && p.Meter_Serial_No == customerMeter.Meter_Serial_No
                                                         select p).ToList();

                        var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                    select p.Description).Distinct();

                        foreach (var description in distinctDescriptions)
                        {
                            var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                            where p.Description == description
                                                            select p).ToList();
                            TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                            if (salesJournalsForThisDesc.Count > 0)
                            {
                                string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                          where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                          select p.Resource_Group_No).FirstOrDefault();

                                switch (resourceGroupNo)
                                {
                                    default:
                                        resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                        break;
                                    case "ELEC":
                                        resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                        break;
                                    case "WATE":
                                        if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                            resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                        else
                                            resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                        break;
                                }
                            }

                            var totalTarrif = (from p in salesJournalsForThisMonth
                                               where p.Description == description
                                               select p.Amount).Sum();


                            // Last reading of previous month
                            var openingReading = (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.Meter_No
                                                  && p.Current_Reading_Date.Date <= lastDayOfPreviousMonth.Date
                                                  && p.Current_reading > 0
                                                  orderby p.Current_Reading_Date descending
                                                  select p).FirstOrDefault();

                            float openingReadingValue = 0;

                            if (openingReading == null)
                            {
                                // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                                openingReadingValue = (from p in serviceUsageLedgerEntries
                                                       where p.Meter_No == customerMeter.Meter_No
                                                       && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                       && p.Current_Reading_Date.Date >= startOfThisMonth.Date
                                                       && p.Current_reading > 0 // Current_reading > 0
                                                       orderby p.Current_Reading_Date ascending
                                                       select p.Previous_Reading).FirstOrDefault();
                            }

                            // Last reading of this month
                            var closingReading = (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.Meter_No
                                                  && p.Current_Reading_Date.Date <= endOfThisMonth.Date
                                                  && p.Current_reading > 0
                                                  orderby p.Current_Reading_Date descending
                                                  select p).FirstOrDefault();

                            TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                            {
                                CustomerNo = customerNo,
                                MeterNo = customerMeter.Meter_No,
                                MeterSerial = customerMeter.Meter_Serial_No,
                                Month = invoiceMonth,
                                Description = description,
                                TotalExVAT = totalTarrif,
                                OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                                ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                ItemResourceType = resourceType
                            };

                            tenantConsumptionStatementItems.Add(statementItem);


                        }

                    }
                }
                else
                {
                    return tenantConsumptionStatementItems;
                }

                #endregion

                #region Fixed Charges

                var salesJournalsForThisMonthFC = (from p in salesJournals
                                                   where p.Posting_Date.Year == invoiceMonth.Year
                                                   && p.Posting_Date.Month == invoiceMonth.Month
                                                   && string.IsNullOrEmpty(p.Meter_Serial_No)
                                                   //&& p.Resource_Group_No == "FC"
                                                   select p).ToList();

                var distinctDescriptionsFC = (from p in salesJournalsForThisMonthFC
                                              select p.Description).Distinct();

                foreach (var description in distinctDescriptionsFC)
                {
                    var totalTarrif = (from p in salesJournalsForThisMonthFC
                                       where p.Description == description
                                       select p.Amount).Sum();

                    // Last reading of previous month
                    var openingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == lastDayOfPreviousMonth.Date
                                                  select p).FirstOrDefault();*/

                    // Last reading of this month
                    var closingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == endOfThisMonth.Date
                                                  select p).FirstOrDefault();*/

                    TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                    {
                        CustomerNo = customerNo,
                        MeterNo = "FC",
                        MeterSerial = "Other Charges",
                        Month = invoiceMonth,
                        Description = description,
                        TotalExVAT = totalTarrif,
                        OpeningReading = openingReading,
                        ClosingReading = closingReading
                    };

                    tenantConsumptionStatementItems.Add(statementItem);
                }


                #endregion

                invoiceMonth = invoiceMonth.AddMonths(1);
            }

            return tenantConsumptionStatementItems;
        }

        public List<TenantConsumptionStatementItem> GetTenantConsumptionInvoice(string customerNo, string companyName, DateTime startDate, DateTime endDate)
        {
            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
            var customer = GetCustomer(customerNo);

            var customerMeters = GetMetersByCustomer(customerNo);
            var salesJournals = GetSalesInvoiceLinesByCustomer(customerNo, startDate.AddDays(-10), endDate.AddDays(10));
            var customerUtilityReadings = GetCustomersUtilityReadings(customerNo, startDate.AddDays(-10), endDate.AddDays(10));
            var serviceUsageLedgerEntries = GetServiceUsageLedgerEntries(customerNo, startDate.AddDays(-10), endDate.AddDays(10));
            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startDate.AddDays(-10), endDate.AddDays(10));

            if (salesJournals.Count == 0 /*|| customerUtilityReadings.Count == 0*/)
                return tenantConsumptionStatementItems;

            List<string> resourceGroupNos = (from p in salesJournals
                                             select p.Resource_Group_No).Distinct().ToList();

            #region Meter Charges

            var serials = (from p in salesJournals
                           where !string.IsNullOrEmpty(p.Meter_Serial_No)
                           select new { p.Meter_Serial_No, p.Meter_No }).Distinct().ToList();

            if (customerMeters.Count > 0)
            {
                foreach (var customerMeter in customerMeters)
                {
                    var salesJournalsForThisMonth = (from p in salesJournals
                                                     where p.Posting_Date.Date >= startDate.Date
                                                     && p.Posting_Date.Date <= endDate.Date
                                                     && p.Meter_Serial_No == customerMeter.Serial_No
                                                     select p).ToList();

                    var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                select p.Description).Distinct();

                    foreach (var description in distinctDescriptions)
                    {
                        var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                        where p.Description == description
                                                        select p).ToList();
                        TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                        if (salesJournalsForThisDesc.Count > 0)
                        {
                            string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                      where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                      select p.Resource_Group_No).FirstOrDefault();

                            switch (resourceGroupNo)
                            {
                                default:
                                    resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                    break;
                                case "ELEC":
                                    resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                    break;
                                case "WATE":
                                    if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                        resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                    else
                                        resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                    break;
                            }
                        }

                        var totalTarrif = (from p in salesJournalsForThisMonth
                                           where p.Description == description
                                           select p.Amount).Sum();


                        // Last reading of previous month
                        var openingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date <= startDate.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        float openingReadingValue = 0;

                        if (openingReading == null)
                        {
                            // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                            openingReadingValue = (from p in serviceUsageLedgerEntries
                                                   where p.Customer_No == customerMeter.Customer_No
                                                   && p.Current_Reading_Date.Date <= endDate.Date
                                                   && p.Current_Reading_Date.Date >= startDate.Date
                                                   && p.Current_reading > 0 // Current_reading > 0
                                                   orderby p.Current_Reading_Date ascending
                                                   select p.Previous_Reading).FirstOrDefault();
                        }

                        // Last reading of this month
                        var closingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date <= endDate.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                        {
                            CustomerNo = customerNo,
                            MeterNo = customerMeter.No,
                            MeterSerial = customerMeter.Serial_No,
                            Month = startDate,
                            Description = description,
                            TotalExVAT = totalTarrif,
                            OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                            ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                            ItemResourceType = resourceType
                        };

                        tenantConsumptionStatementItems.Add(statementItem);


                    }

                }
            }
            else if (serials.Count > 0)
            {
                foreach (var customerMeter in serials)
                {
                    var salesJournalsForThisMonth = (from p in salesJournals
                                                     where p.Posting_Date.Date >= startDate.Date
                                                     && p.Posting_Date.Date <= endDate.Date
                                                     && p.Meter_Serial_No == customerMeter.Meter_Serial_No
                                                     select p).ToList();

                    var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                select p.Description).Distinct();

                    foreach (var description in distinctDescriptions)
                    {
                        var salesJournalsForThisDesc = (from p in salesJournalsForThisMonth
                                                        where p.Description == description
                                                        select p).ToList();
                        TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                        if (salesJournalsForThisDesc.Count > 0)
                        {
                            string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                      where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                      select p.Resource_Group_No).FirstOrDefault();

                            switch (resourceGroupNo)
                            {
                                default:
                                    resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                    break;
                                case "ELEC":
                                    resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                    break;
                                case "WATE":
                                    if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                        resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                    else
                                        resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                    break;
                            }
                        }

                        var totalTarrif = (from p in salesJournalsForThisMonth
                                           where p.Description == description
                                           select p.Amount).Sum();


                        // Last reading of previous month
                        var openingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.Meter_No
                                              && p.Current_Reading_Date.Date <= startDate.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        float openingReadingValue = 0;

                        if (openingReading == null)
                        {
                            // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                            openingReadingValue = (from p in serviceUsageLedgerEntries
                                                   where p.Meter_No == customerMeter.Meter_No
                                                   && p.Current_Reading_Date.Date <= endDate.Date
                                                   && p.Current_Reading_Date.Date >= startDate.Date
                                                   && p.Current_reading > 0 // Current_reading > 0
                                                   orderby p.Current_Reading_Date ascending
                                                   select p.Previous_Reading).FirstOrDefault();
                        }

                        // Last reading of this month
                        var closingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.Meter_No
                                              && p.Current_Reading_Date.Date <= endDate.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                        {
                            CustomerNo = customerNo,
                            MeterNo = customerMeter.Meter_No,
                            MeterSerial = customerMeter.Meter_Serial_No,
                            Month = startDate,
                            Description = description,
                            TotalExVAT = totalTarrif,
                            OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                            ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                            ItemResourceType = resourceType
                        };

                        tenantConsumptionStatementItems.Add(statementItem);


                    }

                }
            }
            else
            {
                return tenantConsumptionStatementItems;
            }

            #endregion

            #region Fixed Charges

            var salesJournalsForThisMonthFC = (from p in salesJournals
                                               where p.Posting_Date.Date >= startDate.Date
                                               && p.Posting_Date.Date <= endDate.Date
                                               && p.Resource_Group_No == "FC"
                                               select p).ToList();

            var distinctDescriptionsFC = (from p in salesJournalsForThisMonthFC
                                          select p.Description).Distinct();

            foreach (var description in distinctDescriptionsFC)
            {
                var totalTarrif = (from p in salesJournalsForThisMonthFC
                                   where p.Description == description
                                   select p.Amount).Sum();

                // Last reading of previous month
                var openingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == lastDayOfPreviousMonth.Date
                                                  select p).FirstOrDefault();*/

                // Last reading of this month
                var closingReading = 0;/* (from p in customerUtilityReadings
                                                  where p.Meter_No == customerMeter.No
                                                  && p.Current_Reading_Date.Date == endOfThisMonth.Date
                                                  select p).FirstOrDefault();*/

                TenantConsumptionStatementItem statementItem = new TenantConsumptionStatementItem()
                {
                    CustomerNo = customerNo,
                    MeterNo = "FC",
                    MeterSerial = "Fixed Charges",
                    Month = startDate,
                    Description = description,
                    TotalExVAT = totalTarrif,
                    OpeningReading = openingReading,
                    ClosingReading = closingReading
                };

                tenantConsumptionStatementItems.Add(statementItem);
            }


            #endregion

            return tenantConsumptionStatementItems;
        }

        public Stream GetTenantConsumptionInvoice(string customerNo, DateTime invoiceMonth, string templateFileName, List<TenantConsumptionStatementItem> tenantConsumptionStatementItems, bool includeBalances = false, string logoPATH = "")
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            var customer = GetCustomer(customerNo);

            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (tenantConsumptionStatementItems.Count == 0)
                return null;

            #region Save Invoice Template

            CExcel.TaxInvoiceTemplate taxInvoiceTemplate = new CExcel.TaxInvoiceTemplate()
            {
                CustomerNo = customerNo,
                AccountName = customer != null ? customer.Customer_Name : "DELETED",
                Address1 = customer != null ? customer.Address : "DELETED",
                Address2 = "",
                Address3 = "",
                BalanceBroughForward = 0,
                BalanceDue = 0,
                EndDate = tenantConsumptionStatementItems.Select(p => p.EndDate).Max().ToString("dd MMMM yyyy"),
                InvoiceNo = $"{customerNo}_{(tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("yyyy_MM"))}",
                PaymentDueDate = DateTime.Now.ToString("dd MMMM yyyy"),
                PaymentsReceived = 0,
                StartDate = tenantConsumptionStatementItems.Select(p => p.StartDate).Min().ToString("dd MMMM yyyy"),
                SupplyAddress = "",
                TaxDate = DateTime.Now.ToString("dd MMMM yyyy"),
                VatNo = "",
                VATOnCurrentCharges = 0.15m,
                Items = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem>(),
                LedgerItems = new List<CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem>(),
                ClosingBalance = 0,
                OpeningBalance = 0,
            };

            if (includeBalances)
            {
                var allCustomerLedgers = GetLedgerEntriesByCustomer(customerNo);
                try
                {
                    taxInvoiceTemplate.OpeningBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum()) * -1.0m;
                }
                catch { }
                try
                {
                    taxInvoiceTemplate.ClosingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum()) * -1.0m;
                }
                catch { }
            }

            foreach (var tenantitem in tenantConsumptionStatementItems)
            {
                taxInvoiceTemplate.Items.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateItem()
                {
                    Description = tenantitem.Description,
                    ClosingReading = tenantitem.ClosingReading,
                    MeterSerial = tenantitem.MeterSerial,
                    OpeningReading = tenantitem.OpeningReading,
                    TotalExVAT = tenantitem.TotalExVAT
                });
            }

            foreach (var ledger in customerLedgerEntries)
            {
                if (ledger.Description.ToUpper().Contains("FEE")
                    || ledger.Document_Type.ToUpper().Contains("PAYMENT"))
                {
                    taxInvoiceTemplate.LedgerItems.Add(new CExcel.TaxInvoiceTemplate.TaxInvoiceTemplateLedgerItem()
                    {
                        Date = ledger.Posting_Date,
                        Description = $"{ledger.Description} - {ledger.Document_No}",
                        TotalExVAT = Convert.ToDecimal(ledger.Amount * -1)
                    });
                }
            }

            taxInvoiceTemplate.PaymentsReceived = taxInvoiceTemplate.LedgerItems.Select(p => p.TotalExVAT).Sum();

            return CExcel.GenerateTaxInvoice(taxInvoiceTemplate, templateFileName, logoPATH: logoPATH);

            #endregion
        }

        public List<TenantConsumptionStatementItemPerDay> GetTenantConsumptionInvoicePerDay(string customerNo, string meterSerial, DateTime invoiceMonth)
        {
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            List<TenantConsumptionStatementItemPerDay> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItemPerDay>();
            var customer = GetCustomer(customerNo);

            var customerMeters = GetMetersByCustomer(customerNo);
            var salesJournals = GetSalesInvoiceLinesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));
            var customerUtilityReadings = GetCustomersUtilityReadings(customerNo, lastDayOfPreviousMonth.AddDays(-10), endOfThisMonth.AddDays(10));
            var customerLedgerEntries = GetLedgerEntriesByCustomer(customerNo, startOfThisMonth.AddDays(-1), endOfThisMonth.AddDays(1));

            if (salesJournals.Count == 0 || customerUtilityReadings.Count == 0)
                return tenantConsumptionStatementItems;

            List<string> resourceGroupNos = (from p in salesJournals
                                             select p.Resource_Group_No).Distinct().ToList();


            foreach (var sJ in salesJournals)
            {
                Console.WriteLine($"{sJ.Posting_Date} - {sJ.Amount} - {sJ.Description}");
            }

            #region Meter Charges

            foreach (var customerMeter in customerMeters)
            {
                if (meterSerial != customerMeter.Serial_No)
                    continue;

                DateTime dtCurrent = startOfThisMonth.Date;

                while (dtCurrent <= endOfThisMonth)
                {
                    var salesJournalsForToday = (from p in salesJournals
                                                 where p.Posting_Date.Date == dtCurrent.Date
                                                 && p.Meter_Serial_No == customerMeter.Serial_No
                                                 select p).ToList();

                    var distinctDescriptions = (from p in salesJournalsForToday
                                                select p.Description).Distinct();

                    foreach (var description in distinctDescriptions)
                    {
                        var salesJournalsForThisDesc = (from p in salesJournalsForToday
                                                        where p.Description == description
                                                        select p).ToList();
                        TenantConsumptionStatementItem.ResourceType resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;

                        if (salesJournalsForThisDesc.Count > 0)
                        {
                            string resourceGroupNo = (from p in salesJournalsForThisDesc
                                                      where !string.IsNullOrEmpty(p.Resource_Group_No)
                                                      select p.Resource_Group_No).FirstOrDefault();

                            switch (resourceGroupNo)
                            {
                                default:
                                    resourceType = TenantConsumptionStatementItem.ResourceType.PAYMENT;
                                    break;
                                case "ELEC":
                                    resourceType = TenantConsumptionStatementItem.ResourceType.ELECTRICITY;
                                    break;
                                case "WATE":
                                    if (description.ToUpper().Contains("Sanitation".ToUpper()))
                                        resourceType = TenantConsumptionStatementItem.ResourceType.SANITATION;
                                    else
                                        resourceType = TenantConsumptionStatementItem.ResourceType.WATER;
                                    break;
                            }
                        }

                        var totalTarrif = (from p in salesJournalsForToday
                                           where p.Description == description
                                           select p.Amount).Sum();


                        // Last reading of previous month
                        var openingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date < dtCurrent.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        // Last reading of this month
                        var closingReading = (from p in customerUtilityReadings
                                              where p.Meter_No == customerMeter.No
                                              && p.Current_Reading_Date.Date <= dtCurrent.Date
                                              && p.Current_reading > 0
                                              orderby p.Current_Reading_Date descending
                                              select p).FirstOrDefault();

                        TenantConsumptionStatementItemPerDay statementItem = new TenantConsumptionStatementItemPerDay()
                        {
                            CustomerNo = customerNo,
                            MeterNo = customerMeter.No,
                            MeterSerial = customerMeter.Serial_No,
                            Month = invoiceMonth,
                            Description = description,
                            TotalExVAT = totalTarrif,
                            OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : 0,
                            ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                            ItemResourceType = resourceType,
                            CurrentDate = dtCurrent
                        };

                        tenantConsumptionStatementItems.Add(statementItem);


                    }

                    dtCurrent = dtCurrent.AddDays(1);
                }
            }

            #endregion

            return tenantConsumptionStatementItems;
        }

        public List<ResourceListItem> GetResourceList()
        {
            var root = Get<ResourceList>("Resources", $"", false);

            var allLedgerEntries = new List<ResourceListItem>();

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<ResourceList>("Resources", $"", false, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<ResourceListItem>();

        }

        public List<ResourceLedgerEntry> GetResourceLedgerEntries(DateTime? startDate, DateTime? endDate)
        {
            StringBuilder sbFilters = new StringBuilder();
            if (startDate.HasValue)
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Posting_Date gt {startDate.Value:yyyy-MM-dd}");
                else
                    sbFilters.Append($" and Posting_Date gt {startDate.Value:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Posting_Date lt {endDate.Value:yyyy-MM-dd}");
                else
                    sbFilters.Append($" and Posting_Date lt {endDate.Value:yyyy-MM-dd}");
            }

            var root = Get<ResourceLedgerEntries>("ResourceLedgerEntries", sbFilters.ToString(), string.IsNullOrEmpty(sbFilters.ToString()) ? false : true);

            var allLedgerEntries = new List<ResourceLedgerEntry>();

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<ResourceLedgerEntries>("ResourceLedgerEntries", sbFilters.ToString(), string.IsNullOrEmpty(sbFilters.ToString()) ? false : true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<ResourceLedgerEntry>();

        }

        public List<ResourceLedgerEntry> GetResourceLedgerEntries(DateTime startDate)
        {
            var root = Get<ResourceLedgerEntries>("ResourceLedgerEntries", $"Posting_Date gt {startDate.AddDays(-1):yyyy-MM-dd}", true);

            var allLedgerEntries = new List<ResourceLedgerEntry>();

            if (root.value != null)
            {
                var entries = root.value.ToList();
                allLedgerEntries.AddRange(entries);

                int count = 1;

                while (entries.Count == 1000)
                {
                    root = Get<ResourceLedgerEntries>("ResourceLedgerEntries", $"Posting_Date gt {startDate.AddDays(-1):yyyy-MM-dd}", true, count * 1000);

                    entries = root.value.ToList();

                    allLedgerEntries.AddRange(entries);

                    count++;
                }

                return allLedgerEntries;
            }

            return new List<ResourceLedgerEntry>();

        }

        public List<ChartOfAccounts.ChartOfAccount> GetChartOfAccounts(DateTime? date = null)
        {
            if (!date.HasValue)
                date = DateTime.Now;

            var allLedgerEntries = new List<ChartOfAccounts.ChartOfAccount>();
            string KEY_ChartOfAccount = $"{_company}_{date.Value:yyyy_MM_dd}_COA";
            if (!_cache.TryGetValue(KEY_ChartOfAccount, out allLedgerEntries))
            {
                var root = Get<ChartOfAccounts>("ChartOfAccounts", $"Date_Filter eq '{date.Value:MM/dd/yy}..C{date.Value:MM/dd/yy}'", true);

                allLedgerEntries = new List<ChartOfAccounts.ChartOfAccount>();

                if (root.value != null)
                {
                    var entries = root.value.ToList();
                    allLedgerEntries.AddRange(entries);

                    int count = 1;

                    while (entries.Count == 1000)
                    {
                        root = Get<ChartOfAccounts>("ChartOfAccounts", $"Date_Filter eq '{date.Value:MM/dd/yy}..C{date.Value:MM/dd/yy}'", true, count * 1000);

                        entries = root.value.ToList();

                        allLedgerEntries.AddRange(entries);

                        count++;
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_ChartOfAccount, allLedgerEntries, cacheEntryOptions);
                }
            }

            return allLedgerEntries;

        }

        public int? CreateJournalEntry(Company company, string customerNo, ServiceReference1.CashReceiptJournal journal, MyVoltageDbContext db, string userID)
        {
            if (journal.Description.Length >= 50)
                journal.Description = journal.Description.Substring(0, 50);

            Data.SkybillJournalLog log = new SkybillJournalLog()
            {
                CompanyID = company.CompanyID,
                CustomerNo = customerNo,
                JournalEntryRequest = journal.ToXML<ServiceReference1.CashReceiptJournal, ServiceReference1.CashReceiptJournal>(),
                JournalEntryRequestStart = DateTime.Now,
                UserID = userID,
                ExceptionDetails = "",
                GetRecIDRequest = "",
                GetRecIDRequestEnd = null,
                GetRecIDRequestStart = null,
                GetRecIDResponse = "",
                JournalEntryRequestEnd = null,
                JournalEntryResponse = "",
                ReceiptJournalRequest = "",
                ReceiptJournalRequestEnd = null,
                ReceiptJournalRequestStart = null,
                ReceiptJournalResponse = "",
            };

            db.Add(log);
            db.SaveChanges();

            log = db.SkybillJournalLogs.Where(p => p.ID == log.ID).SingleOrDefault();
            string endpoint = $"{_BaseURL}:30147/SBUBNZ/WS/{company.Name}/Page/CashReceiptJournal?tenant=1f998066-df97-4de7-875d-0179278815b1";
            string endpoint_PostReceiptJournal = $"{_BaseURL}:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

            try
            {

                ServiceReference1.CashReceiptJournal_PortClient client = new ServiceReference1.CashReceiptJournal_PortClient();
                //  Set the user’s credentials on the proxy  
                client.ClientCredentials.UserName.UserName = _Username;
                client.ClientCredentials.UserName.Password = _Password;
                client.Endpoint.Address = new EndpointAddress(endpoint);

                client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

                ServiceReference1.Create_Result create_Result = null;

                using (new OperationContextScope(client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                    ServiceReference1.Create create = new ServiceReference1.Create("DEFAULT", journal);
                    create_Result = client.CreateAsync(create).Result;

                    log.JournalEntryResponse = create_Result.ToXML<ServiceReference1.Create_Result, ServiceReference1.Create_Result>();
                    log.JournalEntryRequestEnd = DateTime.Now;

                    db.Update(log);
                    db.SaveChanges();

                }

                if (create_Result != null)
                {
                    var crJournal = create_Result.CashReceiptJournal;

                    log.GetRecIDRequest = create_Result.ToXML<ServiceReference1.Create_Result, ServiceReference1.Create_Result>();
                    log.GetRecIDRequestStart = DateTime.Now;
                    db.Update(log);
                    db.SaveChanges();

                    ServiceReference1.GetRecIdFromKey_Result getRecIDResult = null;

                    using (new OperationContextScope(client.InnerChannel))
                    {
                        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        getRecIDResult = client.GetRecIdFromKeyAsync(crJournal.Key).Result;

                        log.GetRecIDResponse = getRecIDResult.ToXML<ServiceReference1.GetRecIdFromKey_Result, ServiceReference1.GetRecIdFromKey_Result>();
                        log.GetRecIDRequestEnd = DateTime.Now;
                        db.Update(log);
                        db.SaveChanges();
                    }

                    if (getRecIDResult != null)
                    {

                        ServiceReference2.WSManagement_PortClient client_PostReceiptJournal = new ServiceReference2.WSManagement_PortClient();

                        client_PostReceiptJournal.ClientCredentials.UserName.UserName = _Username;
                        client_PostReceiptJournal.ClientCredentials.UserName.Password = _Password;
                        client_PostReceiptJournal.Endpoint.Address = new EndpointAddress(endpoint_PostReceiptJournal);
                        client_PostReceiptJournal.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                            new X509ServiceCertificateAuthentication()
                            {
                                CertificateValidationMode = X509CertificateValidationMode.None,
                                RevocationMode = X509RevocationMode.NoCheck
                            };

                        string[] strArr = getRecIDResult.GetRecIdFromKey_Result1.Split(',');

                        string jnlTemplateNameStr = strArr[0].ToString();

                        string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

                        string jnlTemplateName = jnlTemplateNameStrArr[1];

                        string jnlBatchName = strArr[1];
                        int lineNo = Int32.Parse(strArr[2]);

                        ServiceReference2.PostReceiptJournal inValue = new ServiceReference2.PostReceiptJournal()
                        {
                            jnlTemplateName = jnlTemplateName,
                            jnlBatchName = jnlBatchName,
                            lineNo = lineNo,
                        };

                        log.ReceiptJournalRequest = inValue.ToXML<ServiceReference2.PostReceiptJournal, ServiceReference2.PostReceiptJournal>();
                        log.ReceiptJournalRequestStart = DateTime.Now;
                        db.Update(log);
                        db.SaveChanges();

                        using (new OperationContextScope(client_PostReceiptJournal.InnerChannel))
                        {
                            HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                            httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client_PostReceiptJournal.ClientCredentials.UserName.UserName + ":" + client_PostReceiptJournal.ClientCredentials.UserName.Password));
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                            var postReceiptJournalResult = client_PostReceiptJournal.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo).Result;

                            log.ReceiptJournalResponse = postReceiptJournalResult.ToXML<ServiceReference2.PostReceiptJournal_Result, ServiceReference2.PostReceiptJournal_Result>();
                            log.ReceiptJournalRequestEnd = DateTime.Now;
                            db.Update(log);
                            db.SaveChanges();

                            return log.ID;
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                log.ExceptionDetails = $"{ex.Message}{Environment.NewLine}{ex.ToString()}";
                db.Update(log);
                db.SaveChanges();

                // Email/sms Notification
                StringBuilder stringBuilder = new StringBuilder();


                if (log != null)
                    stringBuilder.AppendLine($"Customer:{log.CustomerNo}<br />");
                stringBuilder.AppendLine($"endpoint:{endpoint}<br />");
                stringBuilder.AppendLine($"endpoint_PostReceiptJournal:{endpoint_PostReceiptJournal}<br />");

                PropertyInfo[] properties = log.GetType().GetProperties();

                foreach (var prop in properties)
                {
                    try
                    {
                        var propValue = prop.GetValue(log);
                        if (propValue != null)
                        {
                            string value = propValue.ToString();
                            stringBuilder.AppendLine($"{prop.Name}:{value}<br />");
                        }
                    }
                    catch { }
                }
                stringBuilder.AppendLine($"<br /><br />");
                stringBuilder.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new List<string>() { "madelyn@myvoltage.co.za", "lendl@myvoltage.co.za", "nic@myvoltage.co.za" }.ToArray(), $"Skybill Journal Error", stringBuilder.ToString(), stringBuilder.ToString(), from: "Critical Alerts <criticalalerts@mymetersa.co.za>");

                //SMS.SendSms("27837816268", $"Vending Error - {log.ID}");
                return log.ID;
            }

            return null;
        }

        public int? CreateJournalEntry(Company company, string customerNo, SalesJournal.SalesJnl journal, MyVoltageDbContext db, string userID)
        {
            Data.SkybillJournalLog log = new SkybillJournalLog()
            {
                CompanyID = company.CompanyID,
                CustomerNo = customerNo,
                JournalEntryRequest = journal.ToXML<SalesJournal.SalesJnl, SalesJournal.SalesJnl>(),
                JournalEntryRequestStart = DateTime.Now,
                UserID = userID,
                ExceptionDetails = "",
                GetRecIDRequest = "",
                GetRecIDRequestEnd = null,
                GetRecIDRequestStart = null,
                GetRecIDResponse = "",
                JournalEntryRequestEnd = null,
                JournalEntryResponse = "",
                ReceiptJournalRequest = "",
                ReceiptJournalRequestEnd = null,
                ReceiptJournalRequestStart = null,
                ReceiptJournalResponse = "",
            };

            db.Add(log);
            db.SaveChanges();

            log = db.SkybillJournalLogs.Where(p => p.ID == log.ID).SingleOrDefault();
            try
            {
                string endpoint = $"{_BaseURL}:30147/SBUBNZ/WS/{company.Name}/Page/SalesJnl?tenant=1f998066-df97-4de7-875d-0179278815b1";

                SalesJournal.SalesJnl_PortClient client = new SalesJournal.SalesJnl_PortClient();
                //  Set the user’s credentials on the proxy  
                client.ClientCredentials.UserName.UserName = _Username;
                client.ClientCredentials.UserName.Password = _Password;
                client.Endpoint.Address = new EndpointAddress(endpoint);

                client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

                SalesJournal.Create_Result create_Result = null;

                using (new OperationContextScope(client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                    SalesJournal.Create create = new SalesJournal.Create("DEFAULT", journal);
                    create_Result = client.CreateAsync(create).Result;

                    log.JournalEntryResponse = create_Result.ToXML<SalesJournal.Create_Result, SalesJournal.Create_Result>();
                    log.JournalEntryRequestEnd = DateTime.Now;

                    db.Update(log);
                    db.SaveChanges();

                }

                if (create_Result != null)
                {
                    var sJournal = create_Result.SalesJnl;

                    log.GetRecIDRequest = create_Result.ToXML<SalesJournal.Create_Result, SalesJournal.Create_Result>();
                    log.GetRecIDRequestStart = DateTime.Now;
                    db.Update(log);
                    db.SaveChanges();

                    SalesJournal.GetRecIdFromKey_Result getRecIDResult = null;

                    using (new OperationContextScope(client.InnerChannel))
                    {
                        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        getRecIDResult = client.GetRecIdFromKeyAsync(sJournal.Key).Result;

                        log.GetRecIDResponse = getRecIDResult.ToXML<SalesJournal.GetRecIdFromKey_Result, SalesJournal.GetRecIdFromKey_Result>();
                        log.GetRecIDRequestEnd = DateTime.Now;
                        db.Update(log);
                        db.SaveChanges();
                    }

                    if (getRecIDResult != null)
                    {
                        string endpoint_PostReceiptJournal = $"{_BaseURL}:30147/SBUBNZ/WS/{company.Name}/Codeunit/WSManagement?tenant=1f998066-df97-4de7-875d-0179278815b1";

                        ServiceReference2.WSManagement_PortClient client_PostReceiptJournal = new ServiceReference2.WSManagement_PortClient();

                        client_PostReceiptJournal.ClientCredentials.UserName.UserName = _Username;
                        client_PostReceiptJournal.ClientCredentials.UserName.Password = _Password;
                        client_PostReceiptJournal.Endpoint.Address = new EndpointAddress(endpoint_PostReceiptJournal);
                        client_PostReceiptJournal.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                            new X509ServiceCertificateAuthentication()
                            {
                                CertificateValidationMode = X509CertificateValidationMode.None,
                                RevocationMode = X509RevocationMode.NoCheck
                            };

                        string[] strArr = getRecIDResult.GetRecIdFromKey_Result1.Split(',');

                        string jnlTemplateNameStr = strArr[0].ToString();

                        string[] jnlTemplateNameStrArr = jnlTemplateNameStr.Split(":");

                        string jnlTemplateName = jnlTemplateNameStrArr[1];

                        string jnlBatchName = strArr[1];
                        int lineNo = Int32.Parse(strArr[2]);

                        ServiceReference2.PostReceiptJournal inValue = new ServiceReference2.PostReceiptJournal()
                        {
                            jnlTemplateName = jnlTemplateName,
                            jnlBatchName = jnlBatchName,
                            lineNo = lineNo,
                        };

                        log.ReceiptJournalRequest = inValue.ToXML<ServiceReference2.PostReceiptJournal, ServiceReference2.PostReceiptJournal>();
                        log.ReceiptJournalRequestStart = DateTime.Now;
                        db.Update(log);
                        db.SaveChanges();

                        using (new OperationContextScope(client_PostReceiptJournal.InnerChannel))
                        {
                            HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                            httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client_PostReceiptJournal.ClientCredentials.UserName.UserName + ":" + client_PostReceiptJournal.ClientCredentials.UserName.Password));
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                            var postReceiptJournalResult = client_PostReceiptJournal.PostReceiptJournalAsync(jnlTemplateName.Trim(), jnlBatchName, lineNo).Result;

                            log.ReceiptJournalResponse = postReceiptJournalResult.ToXML<ServiceReference2.PostReceiptJournal_Result, ServiceReference2.PostReceiptJournal_Result>();
                            log.ReceiptJournalRequestEnd = DateTime.Now;
                            db.Update(log);
                            db.SaveChanges();

                            return log.ID;
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                log.ExceptionDetails = ex.ToString();
                db.Update(log);
                db.SaveChanges();

                // Email/sms Notification
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(company.Name);
                stringBuilder.AppendLine("<br />");
                stringBuilder.AppendLine(customerNo);
                stringBuilder.AppendLine("<br />");
                stringBuilder.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new List<string>() { "madelyn@myvoltage.co.za" }.ToArray(), $"Vending Error - {log.ID}", stringBuilder.ToString(), stringBuilder.ToString(), from: "Critical Alerts <criticalalerts@mymetersa.co.za>");

                //SMS.SendSms("27837816268", $"Vending Error - {log.ID}");
            }

            return null;
        }

        public GeneralJournalResult GetGeneralLedgerEntries(DateTime? startDate, DateTime? endDate, string G_L_Account_No, string description, string documentNo = "")
        {
            StringBuilder sbFilters = new StringBuilder();

            if (startDate.HasValue)
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Posting_Date gt {startDate.Value:yyyy-MM-dd}");
                else
                    sbFilters.Append($" and Posting_Date gt {startDate.Value:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Posting_Date lt {endDate.Value:yyyy-MM-dd}");
                else
                    sbFilters.Append($" and Posting_Date lt {endDate.Value:yyyy-MM-dd}");
            }

            if (!string.IsNullOrEmpty(G_L_Account_No))
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"G_L_Account_No eq '{G_L_Account_No}'");
                else
                    sbFilters.Append($" and G_L_Account_No eq '{G_L_Account_No}'");
            }

            if (!string.IsNullOrEmpty(description))
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Description eq '{description}'");
                else
                    sbFilters.Append($" and Description eq '{description}'");
            }

            if (!string.IsNullOrEmpty(documentNo))
            {
                if (string.IsNullOrEmpty(sbFilters.ToString()))
                    sbFilters.Append($"Document_No eq '{documentNo}'");
                else
                    sbFilters.Append($" and Document_No eq '{documentNo}'");
            }

            GeneralJournalResult root = null;

            root = Get<GeneralJournalResult>("GeneralLedgerEntry", sbFilters.ToString(), string.IsNullOrEmpty(sbFilters.ToString()) ? false : true);

            while (root.value == null)
                root = Get<GeneralJournalResult>("GeneralLedgerEntry", sbFilters.ToString(), string.IsNullOrEmpty(sbFilters.ToString()) ? false : true);

            return root;
        }





    }

}
