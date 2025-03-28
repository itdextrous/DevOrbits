using ComaxMgt;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models.BillingViewModels;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class BillingProvider
    {
        private List<Ledger> _ledgerEntries;
        private List<LedgerLineItem> _ledgerLineItems;
        private string _companyName;
        private readonly IMemoryCache _cache;
        private readonly CustomerProvider _customerProvider;

        public BillingProvider(IMemoryCache cache, CustomerProvider customerProvider)
        {
            _companyName = customerProvider.CompanyName;
            _customerProvider = customerProvider;
            _cache = cache;
            string customerNo = customerProvider.CustomerNumber;

            //string key = customerNo + "_" + customerProvider.AccountType;

            //if (!_cache.TryGetValue<List<Ledger>>(key, out _ledgerEntries))
            //{
            _ledgerEntries = GetLedgerEntriesByCustomer(customerNo, customerProvider.AccountType);

            //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
            //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
            //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
            //    cache.Set<List<Ledger>>(key, _ledgerEntries, cacheExpirationOptions);
            //}

            //string ledgerLineItemsKey = key + "_ledgerItem";

            //if (!_cache.TryGetValue<List<LedgerLineItem>>(ledgerLineItemsKey, out _ledgerLineItems))
            //{
            var apiClient = new SkyBillApiClient(_companyName, _cache);
            _ledgerLineItems = apiClient.GetSalesInvoiceLinesByCustomer(customerNo);

            //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
            //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
            //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
            //    cache.Set<List<LedgerLineItem>>(ledgerLineItemsKey, _ledgerLineItems, cacheExpirationOptions);
            //}
        }

        public List<Ledger> GetLedgerEntriesByCustomer()
        {
            return _ledgerEntries;
        }

        private List<Ledger> GetLedgerEntriesByCustomer(string customerNumber, int accountType)
        {
            var apiClient = new SkyBillApiClient(_companyName, _cache);
            List<Ledger> ledgerEntries = apiClient.GetLedgerEntriesByCustomer(customerNumber);
            int multiplier = 1;

            if (accountType == (int)AccountTypeEnum.MyWallet || accountType == (int)AccountTypeEnum.PrepaidCredit)
                multiplier = -1;

            ledgerEntries = ledgerEntries.Select(itm => new Ledger { Document_Type = itm.Document_Type, Original_Amount = itm.Original_Amount * multiplier, Description = itm.Description, Posting_Date = itm.Posting_Date, Document_No = itm.Document_No }).OrderByDescending(itm => itm.Posting_Date).ToList();

            decimal total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount);

            decimal subtract = 0;

            for (int i = 0; i < ledgerEntries.Count; i++)
            {
                total = total - subtract;

                ledgerEntries[i].Balance = total;

                subtract = Convert.ToDecimal(ledgerEntries[i].Original_Amount);
            }

            return ledgerEntries;
        }

        private List<Ledger> GetLedgerLineItemsByMeter(string meterNumber, int accountType)
        {
            List<LedgerLineItem> ledgerLineItems = new List<LedgerLineItem>();
            ledgerLineItems.AddRange(_ledgerLineItems);

            int multiplier = 1;
            if (accountType == (int)AccountTypeEnum.MyWallet || accountType == (int)AccountTypeEnum.PrepaidCredit)
                multiplier = -1;

            var ledgerEntries = ledgerLineItems
                .Where(itm => itm.Meter_Serial_No == meterNumber)
                .Select(itm => new Ledger { Document_Type = itm.Document_Type, Original_Amount = itm.Amount * multiplier, Description = itm.Description, Posting_Date = itm.Posting_Date, Document_No = itm.Document_No }).OrderByDescending(itm => itm.Posting_Date).ToList();

            decimal total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount);

            decimal subtract = 0;

            for (int i = 0; i < ledgerEntries.Count; i++)
            {
                total = total - subtract;

                ledgerEntries[i].Balance = total;

                subtract = Convert.ToDecimal(ledgerEntries[i].Original_Amount);
            }

            return ledgerEntries;
        }

        public MeterJsonModel GetMonthlyInvoiceAmountForRent(string customerNumber, int year, int accountType, bool includeVAT, string meterNumber = "", int month = 0)
        {
            List<string> monthlyLabels = new List<string>();
            List<float> monthlyUsage = new List<float>();

            var ledgerEntries = _ledgerLineItems.Where(l => l.Resource_Group_No == "RENT").ToList();

            // if (!String.IsNullOrEmpty(meterNumber))
            //     ledgerEntries = _ledgerLineItems.Where(l => l.Description == meterNumber).ToList();

            if (ledgerEntries.Count == 0)
                return null;

            Decimal[] monthValues = new Decimal[13];

            DateTime toDate = new DateTime(year, DateTime.Now.Month, 1).AddMonths(1);
            DateTime fromDate = toDate.AddYears(-1);

            if (month != 0)
            {
                fromDate = new DateTime(year, month, 1);
                toDate = new DateTime(year, month, 1).AddMonths(1);
            }

            fromDate = fromDate.AddMonths(-1);
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                monthlyLabels.Add(date.ToString("MM") + " " + date.Year.ToString());

                date = date.AddMonths(1);
            }

            return new MeterJsonModel
            {
                Labels = monthlyLabels,
                Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthValues.ToList(), null, null, null),
                MeterName = "Rent",
                MeterNumber = ledgerEntries[0].Description,
                CurrentCost = monthValues.ToList().Last()
            };
        }

        public MeterJsonModel GetMonthlyInvoiceAmountForRent(List<DateTime> months, bool includeVAT)
        {
            List<string> monthlyLabels = new List<string>();
            List<float> monthlyUsage = new List<float>();

            var ledgerEntries = _ledgerLineItems.Where(l => l.Resource_Group_No == "RENT").ToList();

            if (ledgerEntries.Count == 0)
                return null;

            Decimal[] monthValues = new Decimal[months.Count];

            DateTime toDate = months.Max();
            DateTime fromDate = months.Min();
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                monthlyLabels.Add(date.ToString("MM") + " " + date.Year.ToString());

                date = date.AddMonths(1);
            }

            return new MeterJsonModel
            {
                Labels = monthlyLabels,
                Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthValues.ToList(), null, null, null),
                MeterName = "rent",
                MeterNumber = "rent"
            };
        }

        public List<Decimal> GetMonthlyInvoiceAmount(string customerNumber, int year, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * year) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * year) + 1];

            DateTime toDate = new DateTime(year, DateTime.Now.Month + 1, 1);
            DateTime fromDate = toDate.AddYears(-1);
            fromDate = fromDate.AddMonths(-1);
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetMonthlyInvoiceAmountByMeter(string meterNumber, int year, int accountType, int years, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(meterNumber, accountType);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * years) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * years) + 1];

            DateTime toDate = new DateTime(year, DateTime.Now.Month, 1).AddMonths(1);
            DateTime fromDate = toDate.AddYears(-years);
            DateTime date = fromDate;

            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.Year == date.Year
                && s.Posting_Date.Month == date.Month).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetHistoricalMonthlyInvoiceAmountByMeter(string meterNumber, int year, int accountType, int years, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(meterNumber, accountType);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * years) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * years) + 1];

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            DateTime fromDate = toDate.AddMonths(-((12 * years) + 1));
            DateTime date = fromDate;

            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetMonthlyInvoiceAmount(string customerNumber, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[13];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[24];

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            DateTime fromDate = new DateTime(DateTime.Now.Year - 1, 01, 01);
            int count = 0;


            while (true)
            {
                if (fromDate >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == fromDate.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                fromDate = fromDate.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetDailyInvoiceAmount(string customerNumber, int year, int month, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();
            int days = DateTime.DaysInMonth(year, month);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] dailyBalances = new Decimal[days];

                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            Decimal[] dayValues = new Decimal[days];

            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month + 1, 1);
            DateTime date = fromDate;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("dd/MM/yyyy") == date.ToString("dd/MM/yyyy")).ToList();

                Decimal totalForDay = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForDay = totalForDay * 1.15m;

                dayValues[date.Day - 1] = totalForDay;

                date = date.AddDays(1);
            }

            return dayValues.ToList();
        }

        public List<Decimal> GetDailyInvoiceAmountByMeter(string customerNumber, int year, int month, int accountType, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(customerNumber, accountType);

            int days = DateTime.DaysInMonth(year, month);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] dailyBalances = new Decimal[days];

                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            Decimal[] dayValues = new Decimal[days];

            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month, 1).AddMonths(1);
            DateTime date = fromDate;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("dd/MM/yyyy") == date.ToString("dd/MM/yyyy")).ToList();

                Decimal totalForDay = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForDay = totalForDay * 1.15m;

                dayValues[date.Day - 1] = totalForDay;

                date = date.AddDays(1);
            }

            return dayValues.ToList();
        }

        public List<Decimal> GetDailyBalances(string customerNumber, int year, int month, int accountType)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            if (ledgerEntries.Count == 0)
            {
                int days = DateTime.DaysInMonth(year, month);
                Decimal[] dailyBalances = new Decimal[days];
                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            DateTime endOfTheMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            DateTime toDate = ledgerEntries[0].Posting_Date;
            DateTime fromDate = ledgerEntries[ledgerEntries.Count - 1].Posting_Date;

            if (DateTime.Now.Month == endOfTheMonth.Month && endOfTheMonth > toDate)
            {
                toDate = endOfTheMonth;
                ledgerEntries.Insert(0, new Ledger { Posting_Date = toDate, Balance = ledgerEntries[0].Balance });
            }

            Dictionary<DateTime, Decimal> dailyTotals = new Dictionary<DateTime, Decimal>();
            Decimal[] monthValues = new Decimal[DateTime.DaysInMonth(year, month)];

            decimal runningBalance = 0;
            DateTime date = toDate;

            while (true)
            {
                if (date < fromDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/dd/yyyy") == date.ToString("MM/dd/yyyy")).ToList();

                if (results.Count > 0)
                {
                    runningBalance = results.Max(tbl => tbl.Balance);

                    if (results.Count > 1)
                    {
                        if (dailyTotals.ContainsKey(date))
                        {
                            date = date.AddDays(-1);
                            continue;
                        }

                        dailyTotals.Add(date, runningBalance);
                        runningBalance = results.Min(tbl => tbl.Balance);
                    }
                }

                if (dailyTotals.ContainsKey(date))
                {
                    date = date.AddDays(-1);
                    continue;
                }

                dailyTotals.Add(date, runningBalance);

                date = date.AddDays(-1);
            }

            foreach (KeyValuePair<DateTime, decimal> entry in dailyTotals)
            {
                if (entry.Key.Month == month && entry.Key.Year == year)
                {
                    if (accountType == 1)
                    {
                        monthValues[entry.Key.Day - 1] = (decimal)entry.Value * -1;
                    }
                    else
                    {
                        monthValues[entry.Key.Day - 1] = (decimal)entry.Value;
                    }
                }

            }

            return monthValues.ToList();
        }

        public byte[] GetBillPdf(string documentNo, string company)
        {

            //string endpoint = $"https://20.87.10.102:51348/SBUBNZ/WS/{company}/Codeunit/ComaxMgt?tenant=1f998066-df97-4de7-875d-0179278815b1"; // Azure
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company}/Codeunit/ComaxMgt?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ComaxMgt.ComaxMgt_PortClient client = new ComaxMgt.ComaxMgt_PortClient();
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.Endpoint.EndpointBehaviors.Add(new InspectorBehavior());
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            ComaxMgt.GetBill_Result result = null;

            string blankPdf = "JVBERi0xLjUNCiXi48 / TDQoxIDAgb2JqDQo8PC9UeXBlL1BhZ2UvUmVzb3VyY2VzPDwvRm9udDw8L0YxIDIgMCBSPj4vRXh0R1N0YXRlPDwvR1M3IDMgMCBSL0dTOCA0IDAgUj4 + L1Byb2NTZXRbL1BERi9UZXh0L0ltYWdlQi9JbWFnZUMvSW1hZ2VJXT4 + L01lZGlhQm94WzAgMCA1OTUuMzIwMDEgODQxLjkxOTk4XS9Db250ZW50cyA1IDAgUi9Hcm91cDw8L1R5cGUvR3JvdXAvUy9UcmFuc3BhcmVuY3kvQ1MvRGV2aWNlUkdCPj4vVGFicy9TL1BhcmVudCA2IDAgUj4 + DQplbmRvYmoNCjUgMCBvYmoNCjw8L0ZpbHRlci9GbGF0ZURlY29kZS9MZW5ndGggMTE1Pj5zdHJlYW0NCnicLYy9CsJAEAb7hX2Hr1SLze3lfjZtQAN24kEKSampFDTvD7kEpxsY5svkxG2YZYVD7KK0HhZUOo / fk2k84cPUF6bmolCP8mLaUgdFTJJSQM5BgqG8azTcM + alfjHvZn8bmB4HHCeUK9O57m5MKxR7GCoNCmVuZHN0cmVhbQ0KZW5kb2JqDQo0IDAgb2JqDQo8PC9UeXBlL0V4dEdTdGF0ZS9CTS9Ob3JtYWwvQ0EgMT4 + DQplbmRvYmoNCjMgMCBvYmoNCjw8L1R5cGUvRXh0R1N0YXRlL0JNL05vcm1hbC9jYSAxPj4NCmVuZG9iag0KMiAwIG9iag0KPDwvVHlwZS9Gb250L1N1YnR5cGUvVHJ1ZVR5cGUvTmFtZS9GMS9CYXNlRm9udC9UaW1lc05ld1JvbWFuUFNNVC9FbmNvZGluZy9XaW5BbnNpRW5jb2RpbmcvRm9udERlc2NyaXB0b3IgNyAwIFIvRmlyc3RDaGFyIDMyL0xhc3RDaGFyIDMyL1dpZHRocyA4IDAgUj4 + DQplbmRvYmoNCjggMCBvYmoNClsyNTBdDQplbmRvYmoNCjcgMCBvYmoNCjw8L1R5cGUvRm9udERlc2NyaXB0b3IvRm9udE5hbWUvVGltZXNOZXdSb21hblBTTVQvRmxhZ3MgMzIvSXRhbGljQW5nbGUgMC9Bc2NlbnQgODkxL0Rlc2NlbnQgLTIxNi9DYXBIZWlnaHQgNjkzL0F2Z1dpZHRoIDQwMS9NYXhXaWR0aCAyNjE0L0ZvbnRXZWlnaHQgNDAwL1hIZWlnaHQgMjUwL0xlYWRpbmcgNDIvU3RlbVYgNDAvRm9udEJCb3hbLTU2OCAtMjE2IDIwNDYgNjkzXT4 + DQplbmRvYmoNCjkgMCBvYmoNCjw8L0NyZWF0b3Iod3d3LmNvbnZlcnRhcGkuY29tKS9Qcm9kdWNlcih3d3cuY29udmVydGFwaS5jb20gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICkvQ3JlYXRpb25EYXRlKEQ6MjAxODA1MTQxNzA1NDkrMDMnMDAnKS9Nb2REYXRlKEQ6MjAxODA1MTQxNzA1NDkrMDMnMDAnKT4 + DQplbmRvYmoNCjYgMCBvYmoNCjw8L1R5cGUvUGFnZXMvUGFyZW50IDExIDAgUi9LaWRzWzEgMCBSXS9Db3VudCAxPj4NCmVuZG9iag0KMTEgMCBvYmoNCjw8L0NvdW50IDEvVHlwZS9QYWdlcy9LaWRzWzYgMCBSXT4 + DQplbmRvYmoNCjEyIDAgb2JqDQo8PC9MZW5ndGggMTY3Ni9UeXBlL01ldGFkYXRhL1N1YnR5cGUvWE1MPj5zdHJlYW0NCjw / eHBhY2tldCBiZWdpbj0n77u / JyBpZD0nVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkJz8 + Cjw / YWRvYmUteGFwLWZpbHRlcnMgZXNjPSJDUkxGIj8 + Cjx4OnhtcG1ldGEgeG1sbnM6eD0nYWRvYmU6bnM6bWV0YS8nIHg6eG1wdGs9JzMuMS03MDInPgo8cmRmOlJERiB4bWxuczpyZGY9J2h0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMnPgo8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0nQjA0ODY1RTMtOEM1RS1CRkFBLUE1MzYtMjcxNjZEOTcxQTRBJyB4bWxuczpwZGY9J2h0dHA6Ly9ucy5hZG9iZS5jb20vcGRmLzEuMy8nPjxwZGY6S2V5d29yZHM + PC9wZGY6S2V5d29yZHM + PHBkZjpQcm9kdWNlcj53d3cuY29udmVydGFwaS5jb20gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L3BkZjpQcm9kdWNlcj48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6eG1wPSdodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvJz48eG1wOk1vZGlmeURhdGU + MjAxOC0wNS0xNFQxNzowNTo0OSswMzowMDwveG1wOk1vZGlmeURhdGU + PHhtcDpDcmVhdGVEYXRlPjIwMTgtMDUtMTRUMTc6MDU6NDkrMDM6MDA8L3htcDpDcmVhdGVEYXRlPjx4bXA6TWV0YWRhdGFEYXRlPjIwMTgtMDUtMTRUMTc6MDU6NDkrMDM6MDA8L3htcDpNZXRhZGF0YURhdGU + PHhtcDpDcmVhdG9yVG9vbD53d3cuY29udmVydGFwaS5jb208L3htcDpDcmVhdG9yVG9vbD48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6ZGM9J2h0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvJz48ZGM6Zm9ybWF0PmFwcGxpY2F0aW9uL3BkZjwvZGM6Zm9ybWF0PjxkYzpkZXNjcmlwdGlvbj48cmRmOkFsdD48cmRmOmxpIHhtbDpsYW5nPSd4LWRlZmF1bHQnPjwvcmRmOmxpPjwvcmRmOkFsdD48L2RjOmRlc2NyaXB0aW9uPjxkYzpjcmVhdG9yPjxyZGY6U2VxPjxyZGY6bGk + PC9yZGY6bGk + PC9yZGY6U2VxPjwvZGM6Y3JlYXRvcj48ZGM6dGl0bGU + PHJkZjpBbHQ + PHJkZjpsaSB4bWw6bGFuZz0neC1kZWZhdWx0Jz48L3JkZjpsaT48L3JkZjpBbHQ + PC9kYzp0aXRsZT48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6eG1wTU09J2h0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8nPjx4bXBNTTpEb2N1bWVudElEPnV1aWQ6QUVDN0QxRUUtREJERC1EQkEyLTZBQzMtRTNERTI4QTBERDlGPC94bXBNTTpEb2N1bWVudElEPjx4bXBNTTpJbnN0YW5jZUlEPnV1aWQ6QjA0ODY1RTMtOEM1RS1CRkFBLUE1MzYtMjcxNjZEOTcxQTRBPC94bXBNTTpJbnN0YW5jZUlEPjwvcmRmOkRlc2NyaXB0aW9uPgoKPC9yZGY6UkRGPgo8L3g6eG1wbWV0YT4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAo8P3hwYWNrZXQgZW5kPSd3Jz8 + DQplbmRzdHJlYW0NCmVuZG9iag0KMTAgMCBvYmoNCjw8L0xhbmcoZW4tVVMpL1R5cGUvQ2F0YWxvZy9QYWdlcyAxMSAwIFIvTWV0YWRhdGEgMTIgMCBSPj4NCmVuZG9iag0KeHJlZg0KMCAxMw0KMDAwMDAwMDAwMCA2NTUzNSBmDQowMDAwMDAwMDE3IDAwMDAwIG4NCjAwMDAwMDA1NjcgMDAwMDAgbg0KMDAwMDAwMDUxNCAwMDAwMCBuDQowMDAwMDAwNDYxIDAwMDAwIG4NCjAwMDAwMDAyNzQgMDAwMDAgbg0KMDAwMDAwMTE4MyAwMDAwMCBuDQowMDAwMDAwNzYwIDAwMDAwIG4NCjAwMDAwMDA3MzYgMDAwMDAgbg0KMDAwMDAwMDk5NCAwMDAwMCBuDQowMDAwMDAzMDYzIDAwMDAwIG4NCjAwMDAwMDEyNTEgMDAwMDAgbg0KMDAwMDAwMTMwNiAwMDAwMCBuDQp0cmFpbGVyDQo8PA0KL1NpemUgMTMNCi9Sb290IDEwIDAgUg0KL0luZm8gOSAwIFINCi9JRCBbPEM5ODNEQjY0QjJGQjJDMjFDNURDQzQ5NDZGMURCNDE5PjwzNTdFRjI0RERCRDI1QTlDMkQwNzA4QzVGMkQ5NzRCMz5dDQo + Pg0Kc3RhcnR4cmVmDQozMTQxDQolJUVPRg0K";

            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                try
                {

                    result = client.GetBillAsync(new ComaxMgt.GetBill { documentNo = System.Net.WebUtility.UrlDecode(documentNo), bill = documentNo }).Result;

                }
                catch (Exception ex)
                {
                    result = new GetBill_Result()
                    {
                        bill = blankPdf
                    };
                }
            }

            result.bill = result.bill.Replace(documentNo, "");

            if (result.bill.Length <= 0)
            {
                result.bill = blankPdf;
            }

            return Convert.FromBase64String(result.bill);
        }


        public Stream GetTenantConsumptionInvoice(string customerNo, string companyName, DateTime invoiceMonth, string templateFileName, Data.Company company, Data.Customer localCustomer, bool includeBalances = false, string logoPATH = "")
        {
            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(companyName, _cache, false);
            return skyBillApiClient.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth, templateFileName, company, localCustomer, includeBalances, logoPATH);
        }

        public Stream GetTaxInvoice(Data.Customer localCustomer, Data.Company company, DateTime invoiceMonth, string templateFileName, string logoPATH = "")
        {
            if (localCustomer == null || company == null)
                return null;

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache, false);
            return skyBillApiClient.GetTaxInvoice(localCustomer, company, invoiceMonth, templateFileName, logoPATH);
        }


    }
    public class BillingProvider_Global
    {
        private List<Ledger> _ledgerEntries;
        private List<LedgerLineItem> _ledgerLineItems;
        private string _companyName;
        private readonly IMemoryCache _cache;

        public BillingProvider_Global(IMemoryCache cache, string companyName, string customerNo, int accountType)
        {
            _companyName = companyName;
            _cache = cache;

            //string key = customerNo + "_" + customerProvider.AccountType;

            //if (!_cache.TryGetValue<List<Ledger>>(key, out _ledgerEntries))
            //{
            _ledgerEntries = GetLedgerEntriesByCustomer(customerNo, accountType);

            //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
            //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
            //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
            //    cache.Set<List<Ledger>>(key, _ledgerEntries, cacheExpirationOptions);
            //}

            //string ledgerLineItemsKey = key + "_ledgerItem";

            //if (!_cache.TryGetValue<List<LedgerLineItem>>(ledgerLineItemsKey, out _ledgerLineItems))
            //{
            var apiClient = new SkyBillApiClient(_companyName, _cache);
            _ledgerLineItems = apiClient.GetSalesInvoiceLinesByCustomer(customerNo);

            //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
            //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
            //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
            //    cache.Set<List<LedgerLineItem>>(ledgerLineItemsKey, _ledgerLineItems, cacheExpirationOptions);
            //}
        }

        public List<Ledger> GetLedgerEntriesByCustomer()
        {
            return _ledgerEntries;
        }

        private List<Ledger> GetLedgerEntriesByCustomer(string customerNumber, int accountType)
        {
            var apiClient = new SkyBillApiClient(_companyName, _cache);
            List<Ledger> ledgerEntries = apiClient.GetLedgerEntriesByCustomer(customerNumber);
            int multiplier = 1;

            if (accountType == (int)AccountTypeEnum.MyWallet || accountType == (int)AccountTypeEnum.PrepaidCredit)
                multiplier = -1;

            ledgerEntries = ledgerEntries.Select(itm => new Ledger { Document_Type = itm.Document_Type, Original_Amount = itm.Original_Amount * multiplier, Description = itm.Description, Posting_Date = itm.Posting_Date, Document_No = itm.Document_No }).OrderByDescending(itm => itm.Posting_Date).ToList();

            decimal total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount);

            decimal subtract = 0;

            for (int i = 0; i < ledgerEntries.Count; i++)
            {
                total = total - subtract;

                ledgerEntries[i].Balance = total;

                subtract = Convert.ToDecimal(ledgerEntries[i].Original_Amount);
            }

            return ledgerEntries;
        }

        private List<Ledger> GetLedgerLineItemsByMeter(string meterNumber, int accountType)
        {
            List<LedgerLineItem> ledgerLineItems = new List<LedgerLineItem>();
            ledgerLineItems.AddRange(_ledgerLineItems);

            int multiplier = 1;
            if (accountType == (int)AccountTypeEnum.MyWallet || accountType == (int)AccountTypeEnum.PrepaidCredit)
                multiplier = -1;

            var ledgerEntries = ledgerLineItems
                .Where(itm => itm.Meter_Serial_No == meterNumber)
                .Select(itm => new Ledger { Document_Type = itm.Document_Type, Original_Amount = itm.Amount * multiplier, Description = itm.Description, Posting_Date = itm.Posting_Date, Document_No = itm.Document_No }).OrderByDescending(itm => itm.Posting_Date).ToList();

            decimal total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount);

            decimal subtract = 0;

            for (int i = 0; i < ledgerEntries.Count; i++)
            {
                total = total - subtract;

                ledgerEntries[i].Balance = total;

                subtract = Convert.ToDecimal(ledgerEntries[i].Original_Amount);
            }

            return ledgerEntries;
        }

        public MeterJsonModel GetMonthlyInvoiceAmountForRent(string customerNumber, int year, int accountType, bool includeVAT, string meterNumber = "", int month = 0)
        {
            List<string> monthlyLabels = new List<string>();
            List<float> monthlyUsage = new List<float>();

            var ledgerEntries = _ledgerLineItems.Where(l => l.Resource_Group_No == "RENT").ToList();

            // if (!String.IsNullOrEmpty(meterNumber))
            //     ledgerEntries = _ledgerLineItems.Where(l => l.Description == meterNumber).ToList();

            if (ledgerEntries.Count == 0)
                return null;

            Decimal[] monthValues = new Decimal[13];

            DateTime toDate = new DateTime(year, DateTime.Now.Month, 1).AddMonths(1);
            DateTime fromDate = toDate.AddYears(-1);

            if (month != 0)
            {
                fromDate = new DateTime(year, month, 1);
                toDate = new DateTime(year, month, 1).AddMonths(1);
            }

            fromDate = fromDate.AddMonths(-1);
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                monthlyLabels.Add(date.ToString("MM") + " " + date.Year.ToString());

                date = date.AddMonths(1);
            }

            return new MeterJsonModel
            {
                Labels = monthlyLabels,
                Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthValues.ToList(), null, null, null),
                MeterName = "Rent",
                MeterNumber = ledgerEntries[0].Description,
                CurrentCost = monthValues.ToList().Last()
            };
        }

        public MeterJsonModel GetMonthlyInvoiceAmountForRent(List<DateTime> months, bool includeVAT)
        {
            List<string> monthlyLabels = new List<string>();
            List<float> monthlyUsage = new List<float>();

            var ledgerEntries = _ledgerLineItems.Where(l => l.Resource_Group_No == "RENT").ToList();

            if (ledgerEntries.Count == 0)
                return null;

            Decimal[] monthValues = new Decimal[months.Count];

            DateTime toDate = months.Max();
            DateTime fromDate = months.Min();
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                monthlyLabels.Add(date.ToString("MM") + " " + date.Year.ToString());

                date = date.AddMonths(1);
            }

            return new MeterJsonModel
            {
                Labels = monthlyLabels,
                Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthValues.ToList(), null, null, null),
                MeterName = "rent",
                MeterNumber = "rent"
            };
        }

        public List<Decimal> GetMonthlyInvoiceAmount(string customerNumber, int year, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * year) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * year) + 1];

            DateTime toDate = new DateTime(year, DateTime.Now.Month + 1, 1);
            DateTime fromDate = toDate.AddYears(-1);
            fromDate = fromDate.AddMonths(-1);
            DateTime date = fromDate;
            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetMonthlyInvoiceAmountByMeter(string meterNumber, int year, int accountType, int years, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(meterNumber, accountType);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * years) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * years) + 1];

            DateTime toDate = new DateTime(year, DateTime.Now.Month, 1).AddMonths(1);
            DateTime fromDate = toDate.AddYears(-years);
            DateTime date = fromDate;

            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.Year == date.Year
                && s.Posting_Date.Month == date.Month).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetHistoricalMonthlyInvoiceAmountByMeter(string meterNumber, int year, int accountType, int years, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(meterNumber, accountType);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[(12 * years) + 1];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[(12 * years) + 1];

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            DateTime fromDate = toDate.AddMonths(-((12 * years) + 1));
            DateTime date = fromDate;

            int count = 0;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == date.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                date = date.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetMonthlyInvoiceAmount(string customerNumber, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();

            if (ledgerEntries.Count == 0)
            {
                Decimal[] monthlyBalances = new Decimal[13];

                for (var i = 0; i < 13; i++)
                {
                    monthlyBalances[i] = 0;
                }

                return monthlyBalances.ToList();
            }

            Decimal[] monthValues = new Decimal[24];

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            DateTime fromDate = new DateTime(DateTime.Now.Year - 1, 01, 01);
            int count = 0;


            while (true)
            {
                if (fromDate >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/yyyy") == fromDate.ToString("MM/yyyy")).ToList();

                Decimal totalForMonth = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForMonth = totalForMonth * 1.15m;

                monthValues[count++] = totalForMonth;

                fromDate = fromDate.AddMonths(1);
            }

            return monthValues.ToList();
        }

        public List<Decimal> GetDailyInvoiceAmount(string customerNumber, int year, int month, int accountType, bool includeVAT)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            ledgerEntries = ledgerEntries.Where(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo").ToList();
            int days = DateTime.DaysInMonth(year, month);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] dailyBalances = new Decimal[days];

                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            Decimal[] dayValues = new Decimal[days];

            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month + 1, 1);
            DateTime date = fromDate;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("dd/MM/yyyy") == date.ToString("dd/MM/yyyy")).ToList();

                Decimal totalForDay = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForDay = totalForDay * 1.15m;

                dayValues[date.Day - 1] = totalForDay;

                date = date.AddDays(1);
            }

            return dayValues.ToList();
        }

        public List<Decimal> GetDailyInvoiceAmountByMeter(string customerNumber, int year, int month, int accountType, bool includeVAT)
        {
            var ledgerEntries = GetLedgerLineItemsByMeter(customerNumber, accountType);

            int days = DateTime.DaysInMonth(year, month);

            if (ledgerEntries.Count == 0)
            {
                Decimal[] dailyBalances = new Decimal[days];

                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            Decimal[] dayValues = new Decimal[days];

            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month, 1).AddMonths(1);
            DateTime date = fromDate;

            while (true)
            {
                if (date >= toDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("dd/MM/yyyy") == date.ToString("dd/MM/yyyy")).ToList();

                Decimal totalForDay = results.Sum(tbl => tbl.Original_Amount);

                if (includeVAT)
                    totalForDay = totalForDay * 1.15m;

                dayValues[date.Day - 1] = totalForDay;

                date = date.AddDays(1);
            }

            return dayValues.ToList();
        }

        public List<Decimal> GetDailyBalances(string customerNumber, int year, int month, int accountType)
        {
            var ledgerEntries = new List<Ledger>();
            ledgerEntries.AddRange(_ledgerEntries);

            if (ledgerEntries.Count == 0)
            {
                int days = DateTime.DaysInMonth(year, month);
                Decimal[] dailyBalances = new Decimal[days];
                for (var i = 0; i < days; i++)
                {
                    dailyBalances[i] = 0;
                }

                return dailyBalances.ToList();
            }

            DateTime endOfTheMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            DateTime toDate = ledgerEntries[0].Posting_Date;
            DateTime fromDate = ledgerEntries[ledgerEntries.Count - 1].Posting_Date;

            if (DateTime.Now.Month == endOfTheMonth.Month && endOfTheMonth > toDate)
            {
                toDate = endOfTheMonth;
                ledgerEntries.Insert(0, new Ledger { Posting_Date = toDate, Balance = ledgerEntries[0].Balance });
            }

            Dictionary<DateTime, Decimal> dailyTotals = new Dictionary<DateTime, Decimal>();
            Decimal[] monthValues = new Decimal[DateTime.DaysInMonth(year, month)];

            decimal runningBalance = 0;
            DateTime date = toDate;

            while (true)
            {
                if (date < fromDate)
                    break;

                var results = ledgerEntries.Where(s => s.Posting_Date.ToString("MM/dd/yyyy") == date.ToString("MM/dd/yyyy")).ToList();

                if (results.Count > 0)
                {
                    runningBalance = results.Max(tbl => tbl.Balance);

                    if (results.Count > 1)
                    {
                        if (dailyTotals.ContainsKey(date))
                        {
                            date = date.AddDays(-1);
                            continue;
                        }

                        dailyTotals.Add(date, runningBalance);
                        runningBalance = results.Min(tbl => tbl.Balance);
                    }
                }

                if (dailyTotals.ContainsKey(date))
                {
                    date = date.AddDays(-1);
                    continue;
                }

                dailyTotals.Add(date, runningBalance);

                date = date.AddDays(-1);
            }

            foreach (KeyValuePair<DateTime, decimal> entry in dailyTotals)
            {
                if (entry.Key.Month == month && entry.Key.Year == year)
                {
                    if (accountType == 1)
                    {
                        monthValues[entry.Key.Day - 1] = (decimal)entry.Value * -1;
                    }
                    else
                    {
                        monthValues[entry.Key.Day - 1] = (decimal)entry.Value;
                    }
                }

            }

            return monthValues.ToList();
        }

        public byte[] GetBillPdf(string documentNo, string company)
        {

            //string endpoint = $"https://20.87.10.102:51348/SBUBNZ/WS/{company}/Codeunit/ComaxMgt?tenant=1f998066-df97-4de7-875d-0179278815b1"; // Azure
            string endpoint = $"https://20.87.10.102:30147/SBUBNZ/WS/{company}/Codeunit/ComaxMgt?tenant=1f998066-df97-4de7-875d-0179278815b1";

            ComaxMgt.ComaxMgt_PortClient client = new ComaxMgt.ComaxMgt_PortClient();
            client.ClientCredentials.UserName.UserName = "webservice";
            client.ClientCredentials.UserName.Password = "MyVoltage_001";
            client.Endpoint.Address = new EndpointAddress(endpoint);
            client.Endpoint.EndpointBehaviors.Add(new InspectorBehavior());
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                };

            ComaxMgt.GetBill_Result result = null;

            string blankPdf = "JVBERi0xLjUNCiXi48 / TDQoxIDAgb2JqDQo8PC9UeXBlL1BhZ2UvUmVzb3VyY2VzPDwvRm9udDw8L0YxIDIgMCBSPj4vRXh0R1N0YXRlPDwvR1M3IDMgMCBSL0dTOCA0IDAgUj4 + L1Byb2NTZXRbL1BERi9UZXh0L0ltYWdlQi9JbWFnZUMvSW1hZ2VJXT4 + L01lZGlhQm94WzAgMCA1OTUuMzIwMDEgODQxLjkxOTk4XS9Db250ZW50cyA1IDAgUi9Hcm91cDw8L1R5cGUvR3JvdXAvUy9UcmFuc3BhcmVuY3kvQ1MvRGV2aWNlUkdCPj4vVGFicy9TL1BhcmVudCA2IDAgUj4 + DQplbmRvYmoNCjUgMCBvYmoNCjw8L0ZpbHRlci9GbGF0ZURlY29kZS9MZW5ndGggMTE1Pj5zdHJlYW0NCnicLYy9CsJAEAb7hX2Hr1SLze3lfjZtQAN24kEKSampFDTvD7kEpxsY5svkxG2YZYVD7KK0HhZUOo / fk2k84cPUF6bmolCP8mLaUgdFTJJSQM5BgqG8azTcM + alfjHvZn8bmB4HHCeUK9O57m5MKxR7GCoNCmVuZHN0cmVhbQ0KZW5kb2JqDQo0IDAgb2JqDQo8PC9UeXBlL0V4dEdTdGF0ZS9CTS9Ob3JtYWwvQ0EgMT4 + DQplbmRvYmoNCjMgMCBvYmoNCjw8L1R5cGUvRXh0R1N0YXRlL0JNL05vcm1hbC9jYSAxPj4NCmVuZG9iag0KMiAwIG9iag0KPDwvVHlwZS9Gb250L1N1YnR5cGUvVHJ1ZVR5cGUvTmFtZS9GMS9CYXNlRm9udC9UaW1lc05ld1JvbWFuUFNNVC9FbmNvZGluZy9XaW5BbnNpRW5jb2RpbmcvRm9udERlc2NyaXB0b3IgNyAwIFIvRmlyc3RDaGFyIDMyL0xhc3RDaGFyIDMyL1dpZHRocyA4IDAgUj4 + DQplbmRvYmoNCjggMCBvYmoNClsyNTBdDQplbmRvYmoNCjcgMCBvYmoNCjw8L1R5cGUvRm9udERlc2NyaXB0b3IvRm9udE5hbWUvVGltZXNOZXdSb21hblBTTVQvRmxhZ3MgMzIvSXRhbGljQW5nbGUgMC9Bc2NlbnQgODkxL0Rlc2NlbnQgLTIxNi9DYXBIZWlnaHQgNjkzL0F2Z1dpZHRoIDQwMS9NYXhXaWR0aCAyNjE0L0ZvbnRXZWlnaHQgNDAwL1hIZWlnaHQgMjUwL0xlYWRpbmcgNDIvU3RlbVYgNDAvRm9udEJCb3hbLTU2OCAtMjE2IDIwNDYgNjkzXT4 + DQplbmRvYmoNCjkgMCBvYmoNCjw8L0NyZWF0b3Iod3d3LmNvbnZlcnRhcGkuY29tKS9Qcm9kdWNlcih3d3cuY29udmVydGFwaS5jb20gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICkvQ3JlYXRpb25EYXRlKEQ6MjAxODA1MTQxNzA1NDkrMDMnMDAnKS9Nb2REYXRlKEQ6MjAxODA1MTQxNzA1NDkrMDMnMDAnKT4 + DQplbmRvYmoNCjYgMCBvYmoNCjw8L1R5cGUvUGFnZXMvUGFyZW50IDExIDAgUi9LaWRzWzEgMCBSXS9Db3VudCAxPj4NCmVuZG9iag0KMTEgMCBvYmoNCjw8L0NvdW50IDEvVHlwZS9QYWdlcy9LaWRzWzYgMCBSXT4 + DQplbmRvYmoNCjEyIDAgb2JqDQo8PC9MZW5ndGggMTY3Ni9UeXBlL01ldGFkYXRhL1N1YnR5cGUvWE1MPj5zdHJlYW0NCjw / eHBhY2tldCBiZWdpbj0n77u / JyBpZD0nVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkJz8 + Cjw / YWRvYmUteGFwLWZpbHRlcnMgZXNjPSJDUkxGIj8 + Cjx4OnhtcG1ldGEgeG1sbnM6eD0nYWRvYmU6bnM6bWV0YS8nIHg6eG1wdGs9JzMuMS03MDInPgo8cmRmOlJERiB4bWxuczpyZGY9J2h0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMnPgo8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0nQjA0ODY1RTMtOEM1RS1CRkFBLUE1MzYtMjcxNjZEOTcxQTRBJyB4bWxuczpwZGY9J2h0dHA6Ly9ucy5hZG9iZS5jb20vcGRmLzEuMy8nPjxwZGY6S2V5d29yZHM + PC9wZGY6S2V5d29yZHM + PHBkZjpQcm9kdWNlcj53d3cuY29udmVydGFwaS5jb20gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA8L3BkZjpQcm9kdWNlcj48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6eG1wPSdodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvJz48eG1wOk1vZGlmeURhdGU + MjAxOC0wNS0xNFQxNzowNTo0OSswMzowMDwveG1wOk1vZGlmeURhdGU + PHhtcDpDcmVhdGVEYXRlPjIwMTgtMDUtMTRUMTc6MDU6NDkrMDM6MDA8L3htcDpDcmVhdGVEYXRlPjx4bXA6TWV0YWRhdGFEYXRlPjIwMTgtMDUtMTRUMTc6MDU6NDkrMDM6MDA8L3htcDpNZXRhZGF0YURhdGU + PHhtcDpDcmVhdG9yVG9vbD53d3cuY29udmVydGFwaS5jb208L3htcDpDcmVhdG9yVG9vbD48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6ZGM9J2h0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvJz48ZGM6Zm9ybWF0PmFwcGxpY2F0aW9uL3BkZjwvZGM6Zm9ybWF0PjxkYzpkZXNjcmlwdGlvbj48cmRmOkFsdD48cmRmOmxpIHhtbDpsYW5nPSd4LWRlZmF1bHQnPjwvcmRmOmxpPjwvcmRmOkFsdD48L2RjOmRlc2NyaXB0aW9uPjxkYzpjcmVhdG9yPjxyZGY6U2VxPjxyZGY6bGk + PC9yZGY6bGk + PC9yZGY6U2VxPjwvZGM6Y3JlYXRvcj48ZGM6dGl0bGU + PHJkZjpBbHQ + PHJkZjpsaSB4bWw6bGFuZz0neC1kZWZhdWx0Jz48L3JkZjpsaT48L3JkZjpBbHQ + PC9kYzp0aXRsZT48L3JkZjpEZXNjcmlwdGlvbj4KPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9J0IwNDg2NUUzLThDNUUtQkZBQS1BNTM2LTI3MTY2RDk3MUE0QScgeG1sbnM6eG1wTU09J2h0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8nPjx4bXBNTTpEb2N1bWVudElEPnV1aWQ6QUVDN0QxRUUtREJERC1EQkEyLTZBQzMtRTNERTI4QTBERDlGPC94bXBNTTpEb2N1bWVudElEPjx4bXBNTTpJbnN0YW5jZUlEPnV1aWQ6QjA0ODY1RTMtOEM1RS1CRkFBLUE1MzYtMjcxNjZEOTcxQTRBPC94bXBNTTpJbnN0YW5jZUlEPjwvcmRmOkRlc2NyaXB0aW9uPgoKPC9yZGY6UkRGPgo8L3g6eG1wbWV0YT4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAo8P3hwYWNrZXQgZW5kPSd3Jz8 + DQplbmRzdHJlYW0NCmVuZG9iag0KMTAgMCBvYmoNCjw8L0xhbmcoZW4tVVMpL1R5cGUvQ2F0YWxvZy9QYWdlcyAxMSAwIFIvTWV0YWRhdGEgMTIgMCBSPj4NCmVuZG9iag0KeHJlZg0KMCAxMw0KMDAwMDAwMDAwMCA2NTUzNSBmDQowMDAwMDAwMDE3IDAwMDAwIG4NCjAwMDAwMDA1NjcgMDAwMDAgbg0KMDAwMDAwMDUxNCAwMDAwMCBuDQowMDAwMDAwNDYxIDAwMDAwIG4NCjAwMDAwMDAyNzQgMDAwMDAgbg0KMDAwMDAwMTE4MyAwMDAwMCBuDQowMDAwMDAwNzYwIDAwMDAwIG4NCjAwMDAwMDA3MzYgMDAwMDAgbg0KMDAwMDAwMDk5NCAwMDAwMCBuDQowMDAwMDAzMDYzIDAwMDAwIG4NCjAwMDAwMDEyNTEgMDAwMDAgbg0KMDAwMDAwMTMwNiAwMDAwMCBuDQp0cmFpbGVyDQo8PA0KL1NpemUgMTMNCi9Sb290IDEwIDAgUg0KL0luZm8gOSAwIFINCi9JRCBbPEM5ODNEQjY0QjJGQjJDMjFDNURDQzQ5NDZGMURCNDE5PjwzNTdFRjI0RERCRDI1QTlDMkQwNzA4QzVGMkQ5NzRCMz5dDQo + Pg0Kc3RhcnR4cmVmDQozMTQxDQolJUVPRg0K";

            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(client.ClientCredentials.UserName.UserName + ":" + client.ClientCredentials.UserName.Password));
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                try
                {

                    result = client.GetBillAsync(new ComaxMgt.GetBill { documentNo = System.Net.WebUtility.UrlDecode(documentNo), bill = documentNo }).Result;

                }
                catch (Exception ex)
                {
                    result = new GetBill_Result()
                    {
                        bill = blankPdf
                    };
                }
            }

            result.bill = result.bill.Replace(documentNo, "");

            if (result.bill.Length <= 0)
            {
                result.bill = blankPdf;
            }

            return Convert.FromBase64String(result.bill);
        }


        public Stream GetTenantConsumptionInvoice(string customerNo, string companyName, DateTime invoiceMonth, string templateFileName, Data.Company company, Data.Customer localCustomer, bool includeBalances = false, string logoPATH = "")
        {
            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(companyName, _cache, false);
            return skyBillApiClient.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth, templateFileName, company, localCustomer, includeBalances, logoPATH);
        }

        public Stream GetTaxInvoice(Data.Customer localCustomer, Data.Company company, DateTime invoiceMonth, string templateFileName, string logoPATH = "")
        {
            if (localCustomer == null || company == null)
                return null;

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache, false);
            return skyBillApiClient.GetTaxInvoice(localCustomer, company, invoiceMonth, templateFileName, logoPATH);
        }


    }
}
