using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.NetcashJobs
{
    //public class CompanyBalanceUpdates
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public CompanyBalanceUpdates(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var companies = db.Companies.ToList();

    //        foreach (var c in companies.Where(p => !string.IsNullOrEmpty(p.MasterServiceKey)).ToList())
    //        {
    //            var intermStatement = MyVoltage.Api.NetcashAPI.GetInterimStatement(c.MasterServiceKey);
    //            if (intermStatement != null && intermStatement.ClosingBalance != null)
    //            {
    //                var cToUpdate = db.Companies.Where(p => p.CompanyID == c.CompanyID).SingleOrDefault();
    //                cToUpdate.NetcashBalance = intermStatement.ClosingBalance.Effect == "+" ? intermStatement.ClosingBalance.Amount : intermStatement.ClosingBalance.Amount * -1.0m;
    //                cToUpdate.NetcashBalanceDate = DateTime.Now;
    //                db.Update(cToUpdate);
    //                db.SaveChanges();
    //                continue;
    //            }

    //            if (c.NetcashBalanceDate.HasValue && c.NetcashBalanceDate.Value.Date == DateTime.Now.Date)
    //                continue;

    //            var statement = MyVoltage.Api.NetcashAPI.GetStatement(c.MasterServiceKey, DateTime.Now.AddDays(-1));
    //            if (statement != null && statement.ClosingBalance != null)
    //            {
    //                var cToUpdate = db.Companies.Where(p => p.CompanyID == c.CompanyID).SingleOrDefault();
    //                cToUpdate.NetcashBalance = statement.ClosingBalance.Effect == "+" ? statement.ClosingBalance.Amount : statement.ClosingBalance.Amount * -1.0m;
    //                cToUpdate.NetcashBalanceDate = DateTime.Now.Date;
    //                db.Update(cToUpdate);
    //                db.SaveChanges();
    //                continue;
    //            }


    //        }

    //    }



    //}

    //public class StatementsSync
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public StatementsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var companies = db.Companies.ToList();

    //        DateTime stopDate = DateTime.Now.AddDays(-10).Date;

    //        DateTime current = DateTime.Now.AddDays(-1).Date;
    //        while (current.Date > stopDate.Date)
    //        {
    //            foreach (var c in companies.Where(p => !string.IsNullOrEmpty(p.MasterServiceKey)).ToList())
    //            {
    //                //Console.WriteLine($"\r\n\r\n\t{c.Name} - {current.ToDateShort()}\r\n\r\n");

    //                var netcashStatements = (from p in db.NetcashStatements
    //                                         where p.CompanyID == c.CompanyID
    //                                         && p.Date.Date == current.Date
    //                                         select p).ToList();

    //                var statement = MyVoltage.Api.NetcashAPI.GetStatement(c.MasterServiceKey, current);
    //                if (statement != null)
    //                {
    //                    if (statement.StatementItems.Count == 0 && statement.OpeningBalance.Amount == 0 && statement.ClosingBalance.Amount == 0)
    //                        break;

    //                    if (statement.OpeningBalance != null)
    //                    {
    //                        var existingOpening = (from p in netcashStatements
    //                                               where p.InternalDBID == Convert.ToInt32(statement.OpeningBalance.InternalDBID)
    //                                               && p.Amount == statement.OpeningBalance.Amount
    //                                               && p.CompanyID == c.CompanyID
    //                                               && p.Date == statement.OpeningBalance.Date
    //                                               && p.Description == statement.OpeningBalance.Description
    //                                               && p.TransactionCode == statement.OpeningBalance.TransactionCode
    //                                               select p).SingleOrDefault();

    //                        if (existingOpening == null)
    //                        {
    //                            Data.NetcashStatement netcashStatement = new Data.NetcashStatement()
    //                            {
    //                                Amount = statement.OpeningBalance.Amount,
    //                                CompanyID = c.CompanyID,
    //                                Date = statement.OpeningBalance.Date,
    //                                DateSynced = DateTime.Now,
    //                                Description = statement.OpeningBalance.Description,
    //                                InternalDBID = Convert.ToInt32(statement.OpeningBalance.InternalDBID),
    //                                InternalIndicator = statement.OpeningBalance.InternalIndicator,
    //                                LedgerAccountAffected = statement.OpeningBalance.LedgerAccountAffected,
    //                                PaymentID = null,
    //                                SkybillDocumentNo = "",
    //                                SkybillGLNo = null,
    //                                TransactionCode = statement.OpeningBalance.TransactionCode,
    //                            };
    //                            db.Add(netcashStatement);
    //                            db.SaveChanges();
    //                        }
    //                    }

    //                    foreach (var item in statement.StatementItems)
    //                    {
    //                        var existing = (from p in netcashStatements
    //                                        where p.InternalDBID == Convert.ToInt32(item.InternalDBID)
    //                                        && p.Amount == item.Amount
    //                                        && p.CompanyID == c.CompanyID
    //                                        && p.Date == item.Date
    //                                        && p.Description == item.Description
    //                                        && p.TransactionCode == item.TransactionCode
    //                                        select p).SingleOrDefault();

    //                        if (existing == null)
    //                        {
    //                            Data.NetcashStatement netcashStatement = new Data.NetcashStatement()
    //                            {
    //                                Amount = item.Amount,
    //                                CompanyID = c.CompanyID,
    //                                Date = item.Date,
    //                                DateSynced = DateTime.Now,
    //                                Description = item.Description,
    //                                InternalDBID = Convert.ToInt32(item.InternalDBID),
    //                                InternalIndicator = item.InternalIndicator,
    //                                LedgerAccountAffected = item.LedgerAccountAffected,
    //                                PaymentID = null,
    //                                SkybillDocumentNo = "",
    //                                SkybillGLNo = null,
    //                                TransactionCode = item.TransactionCode,
    //                            };
    //                            db.Add(netcashStatement);
    //                            db.SaveChanges();
    //                        }
    //                    }

    //                    if (statement.ClosingBalance != null)
    //                    {
    //                        var existingClosing = (from p in netcashStatements
    //                                               where p.InternalDBID == Convert.ToInt32(statement.ClosingBalance.InternalDBID)
    //                                               && p.Amount == statement.ClosingBalance.Amount
    //                                               && p.CompanyID == c.CompanyID
    //                                               && p.Date == statement.ClosingBalance.Date
    //                                               && p.Description == statement.ClosingBalance.Description
    //                                               && p.TransactionCode == statement.ClosingBalance.TransactionCode
    //                                               select p).SingleOrDefault();

    //                        if (existingClosing == null)
    //                        {
    //                            Data.NetcashStatement netcashStatement = new Data.NetcashStatement()
    //                            {
    //                                Amount = statement.ClosingBalance.Amount,
    //                                CompanyID = c.CompanyID,
    //                                Date = statement.ClosingBalance.Date,
    //                                DateSynced = DateTime.Now,
    //                                Description = statement.ClosingBalance.Description,
    //                                InternalDBID = Convert.ToInt32(statement.ClosingBalance.InternalDBID),
    //                                InternalIndicator = statement.ClosingBalance.InternalIndicator,
    //                                LedgerAccountAffected = statement.ClosingBalance.LedgerAccountAffected,
    //                                PaymentID = null,
    //                                SkybillDocumentNo = "",
    //                                SkybillGLNo = null,
    //                                TransactionCode = statement.ClosingBalance.TransactionCode,
    //                            };
    //                            db.Add(netcashStatement);
    //                            db.SaveChanges();
    //                        }
    //                    }

    //                }
    //            }

    //            current = current.AddDays(-1);
    //        }


    //    }



    //}

    //public class StatementsSkybillAndPaymentsSync
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public StatementsSkybillAndPaymentsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var companies = db.Companies.ToList();
    //        var customers = db.Customers.ToList();

    //        foreach (var c in companies)
    //        {
    //            var netcashStatementsForPaymentLookups = (from p in db.NetcashStatements
    //                                                      where p.CompanyID == c.CompanyID
    //                                                      && p.TransactionCode != "OBL"
    //                                                      && p.TransactionCode != "CBL"
    //                                                      && !p.PaymentID.HasValue
    //                                                      select p).ToList();

    //            Console.WriteLine($"\r\n\r\n\t{c.Name} - {netcashStatementsForPaymentLookups.Count()}\r\n\r\n");
    //            foreach (var netcashStatementsForPayment in netcashStatementsForPaymentLookups)
    //            {
    //                var payment = (from p in db.Payments
    //                               where p.RequestTrace.EndsWith(netcashStatementsForPayment.InternalDBID.ToString())
    //                               select p).SingleOrDefault();

    //                if (payment != null)
    //                {
    //                    var netcashStatement = db.NetcashStatements.Where(p => p.ID == netcashStatementsForPayment.ID).SingleOrDefault();
    //                    netcashStatement.PaymentID = payment.PaymentID;
    //                    db.Update(netcashStatement);
    //                    db.SaveChanges();
    //                }
    //            }

    //            var netcashStatementsForSkybillLookups = (from p in db.NetcashStatements
    //                                                      where p.CompanyID == c.CompanyID
    //                                                      && p.TransactionCode != "OBL"
    //                                                      && p.TransactionCode != "CBL"
    //                                                      && p.PaymentID.HasValue
    //                                                      && (!string.IsNullOrEmpty(p.SkybillDocumentNo) || !p.SkybillGLNo.HasValue)
    //                                                      select p).ToList();

    //            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(c.Name, _cache);
    //            Console.WriteLine($"\r\n\r\n\t{c.Name} - {netcashStatementsForSkybillLookups.Count()}\r\n\r\n");
    //            foreach (var netcashStatementsForSkybill in netcashStatementsForSkybillLookups)
    //            {
    //                var payment = (from p in db.Payments
    //                               where p.RequestTrace.EndsWith(netcashStatementsForSkybill.InternalDBID.ToString())
    //                               select p).SingleOrDefault();

    //                string desc = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment";
    //                var ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
    //                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
    //                {
    //                    if (ledger.value.Length == 1)
    //                    {
    //                        var netcashStatement = db.NetcashStatements.Where(p => p.ID == netcashStatementsForSkybill.ID).SingleOrDefault();
    //                        netcashStatement.SkybillDocumentNo = ledger.value[0].Document_No;
    //                        try { netcashStatement.SkybillGLNo = Convert.ToInt32(ledger.value[0].Bal_Account_No); }
    //                        catch { }
    //                        db.Update(netcashStatement);
    //                        db.SaveChanges();
    //                    }
    //                }

    //            }

    //        }

    //    }



    //}

    //public class StatementsManualPaymentsSync
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public StatementsManualPaymentsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunEFTSync();
    //        RunApprovalsSync();
    //    }

    //    public async void RunEFTSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var companies = db.Companies.ToList();
    //        var customers = db.Customers.ToList();
    //        var sbCustomers = db.SkybillCustomers.ToList();
    //        var netcashManualPayments = db.NetcashManualPayments.ToList();

    //        foreach (var c in companies)
    //        {
    //            var netcashStatementsForPaymentLookups = (from p in db.NetcashStatements
    //                                                      where p.CompanyID == c.CompanyID
    //                                                      && p.Date.Date >= DateTime.Now.AddMonths(-1).Date
    //                                                      && p.TransactionCode == "PCD"
    //                                                      && string.IsNullOrEmpty(p.SkybillDocumentNo)
    //                                                      select p).ToList();

    //            var companySbCustomers = sbCustomers.Where(p => p.CompanyID == c.CompanyID).ToList();

    //            Console.WriteLine($"\r\n\r\n\t{c.Name} - {netcashStatementsForPaymentLookups.Count()}\r\n\r\n");
    //            foreach (var netcashStatementsForPayment in netcashStatementsForPaymentLookups)
    //            {
    //                var existing = (from p in netcashManualPayments
    //                                where p.NetcashStatementID == netcashStatementsForPayment.ID
    //                                select p).SingleOrDefault();

    //                if (existing == null)
    //                {
    //                    var sC = (from p in companySbCustomers
    //                              where p.Customer_No.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.Customer_No.ToUpper())
    //                              || p.Serial_No.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.Serial_No.ToUpper())
    //                              || p.AuxiliaryIndex1.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.AuxiliaryIndex1.ToUpper())
    //                              || p.AuxiliaryIndex2.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.AuxiliaryIndex2.ToUpper())
    //                              || p.AuxiliaryIndex3.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.AuxiliaryIndex3.ToUpper())
    //                              || p.AuxiliaryIndex4.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.AuxiliaryIndex4.ToUpper())
    //                              || p.AuxiliaryIndex5.ToUpper().Contains(netcashStatementsForPayment.Description.ToUpper())
    //                              || netcashStatementsForPayment.Description.ToUpper().Contains(p.AuxiliaryIndex5.ToUpper())
    //                              select p).FirstOrDefault();

    //                    if (sC != null)
    //                    {
    //                        Data.NetcashManualPayment netcashManualPayment = new NetcashManualPayment()
    //                        {
    //                            Approved = null,
    //                            ApprovedBy = "",
    //                            ApprovedDate = null,
    //                            CompanyID = c.CompanyID,
    //                            CustomerNo = sC.Customer_No,
    //                            DateCreated = DateTime.Now,
    //                            NetcashStatementID = netcashStatementsForPayment.ID,
    //                            SkybillJournalLogID = null,
    //                            SkybillCompanyName = "",
    //                            SkybillCustomerNo = "",
    //                            Vending1Amount5621 = null,
    //                            Vending1Amount7191 = null,
    //                            Vending1Amount8640 = null,
    //                            Vending1LogID = null,
    //                            Vending1Required = true,
    //                        };

    //                        db.Add(netcashManualPayment);
    //                        db.SaveChanges();

    //                    }

    //                }
    //            }

    //        }

    //    }

    //    public async void RunApprovalsSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var companies = db.Companies.ToList();
    //        var customers = db.Customers.ToList();
    //        var sbCustomers = db.SkybillCustomers.ToList();
    //        var netcashManualPayments = (from p in db.NetcashManualPayments
    //                                     where p.Approved.HasValue && p.Approved.Value
    //                                     && !p.SkybillJournalLogID.HasValue
    //                                     select p).ToList();

    //        foreach (var netcashManPay in netcashManualPayments)
    //        {
    //            var company = companies.Where(p => p.CompanyID == netcashManPay.CompanyID).SingleOrDefault();
    //            var netcashStatement = db.NetcashStatements.Where(p => p.ID == netcashManPay.NetcashStatementID).SingleOrDefault();

    //            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

    //            var logID = skyBillApiClient.CreateJournalEntry(company,
    //                netcashManPay.CustomerNo,
    //                new ServiceReference1.CashReceiptJournal()
    //                {
    //                    Posting_DateSpecified = true,
    //                    Posting_Date = DateTime.UtcNow.Date,
    //                    Document_TypeSpecified = true,
    //                    Document_Type = ServiceReference1.Document_Type.Payment,
    //                    Account_TypeSpecified = true,
    //                    Account_Type = ServiceReference1.Account_Type.Customer,
    //                    Account_No = netcashManPay.CustomerNo,
    //                    AmountSpecified = true,
    //                    Description = $"{netcashStatement.InternalDBID}: Direct Deposit",
    //                    Amount = netcashStatement.Amount * -1,
    //                    Bal_Account_TypeSpecified = true,
    //                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
    //                    Bal_Account_No = "SAGEPAY",
    //                },
    //                db,
    //                netcashManPay.ApprovedBy);

    //            if (logID.HasValue)
    //            {
    //                var itemToUpdate = db.NetcashManualPayments.Where(p => p.ID == netcashManPay.ID).SingleOrDefault();
    //                itemToUpdate.SkybillJournalLogID = logID.Value;
    //                db.Update(itemToUpdate);
    //                db.SaveChanges();
    //            }

    //            #region Vending

    //            var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.NetcashManualPayment, netcashStatement.Amount, netcashStatement.InternalDBID.ToString());

    //            var vendingLogID = skyBillApiClient.CreateJournalEntry(company, netcashManPay.CustomerNo, new ServiceReference1.CashReceiptJournal()
    //            {
    //                Posting_DateSpecified = true,
    //                Posting_Date = DateTime.UtcNow.Date,
    //                Document_TypeSpecified = true,
    //                Document_Type = ServiceReference1.Document_Type.Payment,
    //                Account_TypeSpecified = true,

    //                Account_Type = ServiceReference1.Account_Type.G_L_Account,
    //                Account_No = "8640",

    //                AmountSpecified = true,
    //                Description = bankCharges.FixedFeeDescription,
    //                Amount = bankCharges.FixedFee,
    //                Bal_Account_TypeSpecified = true,

    //                Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
    //                Bal_Account_No = "SAGEPAY"
    //            }, db, netcashManPay.ApprovedBy);

    //            if (vendingLogID.HasValue)
    //            {
    //                var itemToUpdate = db.NetcashManualPayments.Where(p => p.ID == netcashManPay.ID).SingleOrDefault();
    //                itemToUpdate.Vending1LogID = vendingLogID.Value;
    //                db.Update(itemToUpdate);
    //                db.SaveChanges();
    //            }

    //            #endregion


    //        }

    //    }



    //}

}
