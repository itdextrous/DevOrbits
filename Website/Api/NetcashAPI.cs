using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api
{
    public class NetcashAPI
    {
        public class Statement
        {
            public StatementItem OpeningBalance { get; set; }
            public List<StatementItem> StatementItems { get; set; }
            public StatementItem ClosingBalance { get; set; }

            public class StatementItem
            {
                public DateTime Date { get; set; }
                public string TransactionCode { get; set; }
                public string TransactionCodeDescription
                {
                    get
                    {
                        if (!string.IsNullOrEmpty(TransactionCode))
                        {
                            switch (TransactionCode)
                            {
                                case "PNP": return "Retail payment";
                                case "PNQ": return "Retail payment return";
                                case "PNM": return "Scan to Pay payment";
                                case "PNW": return "Scan to Pay declined";
                                case "PNE": return "EFT payment";
                                case "PNZ": return "EFT return";
                                case "PNA": return "Credit Card authorize";
                                case "PNC": return "Credit Card payment";
                                case "PNU": return "Credit Card declined";
                                case "PND": return "Credit Card dispute";
                                case "PNR": return "Credit Card refund";
                                case "PIA": return "Ozow Auth";
                                case "PIS": return "Ozow Success";
                                case "PIF": return "Ozow Failure";
                                case "PIR": return "Ozow Recall";
                                case "PVC": return "Visa CheckOut Payment";
                                case "PVU": return "Visa CheckOut Decline";
                                case "PVR": return "Visa CheckOut Refund";
                                case "PVD": return "Visa CheckOut Dispute";
                                case "PQR": return "Masterpass QR";
                                case "PCD": return "Client Deposit";
                            }
                        }
                        return "";
                    }
                }

                public string InternalDBID { get; set; }
                public string Description { get; set; }
                public decimal Amount { get; set; }
                public string Effect { get; set; }
                public string InternalIndicator { get; set; }
                public string LedgerAccountAffected { get; set; }
            }
        }

        public static Statement GetInterimStatement(string ServiceKey)
        {
            NetcashWS.NIWS_NIFClient nIWS_NIFClient = new NetcashWS.NIWS_NIFClient();

            var requestResult = nIWS_NIFClient.RequestInterimMerchantStatementAsync(ServiceKey);

            var statementResult = nIWS_NIFClient.RetrieveMerchantStatementAsync(ServiceKey, requestResult.Result);

            int maxRetryCoount = 100;
            int retryCount = 0;

            while (statementResult.Result == "FILE NOT READY")
            {
                retryCount++;
                System.Threading.Thread.Sleep(1000);
                statementResult = nIWS_NIFClient.RetrieveMerchantStatementAsync(ServiceKey, requestResult.Result);

                if (retryCount >= maxRetryCoount)
                    return null;
            }

            if (statementResult.Result == "NO CHANGE"
                || statementResult.Result == "100"
                || statementResult.Result == "200"
                || string.IsNullOrEmpty(statementResult.Result)
                )
                return null;

            Statement statement = new Statement()
            {
                StatementItems = new List<Statement.StatementItem>(),
            };


            string[] fileLines = statementResult.Result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string fileLine in fileLines)
            {
                string[] lineVars = fileLine.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                Statement.StatementItem statementItem = new Statement.StatementItem()
                {
                    Date = Convert.ToDateTime(lineVars[0]),
                    TransactionCode = lineVars[1],
                    InternalDBID = lineVars[2],
                    Description = lineVars[3],
                    Amount = Convert.ToDecimal(lineVars[4]),
                    Effect = lineVars[5],
                    InternalIndicator = lineVars[6],
                    LedgerAccountAffected = lineVars[7],
                };

                if (statementItem.TransactionCode == "OBL")
                {
                    statement.OpeningBalance = statementItem;
                }
                else if (statementItem.TransactionCode == "CBL")
                {
                    statement.ClosingBalance = statementItem;
                }
                else
                {
                    statement.StatementItems.Add(statementItem);
                }

            }

            return statement;
        }

        public static Statement GetStatement(string ServiceKey, DateTime dateToGet)
        {
            NetcashWS.NIWS_NIFClient nIWS_NIFClient = new NetcashWS.NIWS_NIFClient();

            var requestResult = nIWS_NIFClient.RequestMerchantStatementAsync(ServiceKey, dateToGet.ToString("yyyyMMdd"));

            var statementResult = nIWS_NIFClient.RetrieveMerchantStatementAsync(ServiceKey, requestResult.Result);

            int maxRetryCoount = 100;
            int retryCount = 0;

            while (statementResult.Result == "FILE NOT READY")
            {
                retryCount++;
                System.Threading.Thread.Sleep(1000);
                statementResult = nIWS_NIFClient.RetrieveMerchantStatementAsync(ServiceKey, requestResult.Result);

                if (retryCount >= maxRetryCoount)
                    return null;
            }


            if (statementResult.Result == "NO CHANGE"
                || statementResult.Result == "100"
                || statementResult.Result == "200"
                || string.IsNullOrEmpty(statementResult.Result)
                )
                return null;

            Statement statement = new Statement()
            {
                StatementItems = new List<Statement.StatementItem>(),
            };


            string[] fileLines = statementResult.Result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string fileLine in fileLines)
            {
                string[] lineVars = fileLine.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                Statement.StatementItem statementItem = new Statement.StatementItem()
                {
                    Date = Convert.ToDateTime(lineVars[0]),
                    TransactionCode = lineVars[1],
                    InternalDBID = lineVars[2],
                    Description = lineVars[3],
                    Amount = Convert.ToDecimal(lineVars[4]),
                    Effect = lineVars[5],
                    InternalIndicator = lineVars[6],
                    LedgerAccountAffected = lineVars[7],
                };

                if (statementItem.TransactionCode == "OBL")
                {
                    statement.OpeningBalance = statementItem;
                }
                else if (statementItem.TransactionCode == "CBL")
                {
                    statement.ClosingBalance = statementItem;
                }
                else
                {
                    statement.StatementItems.Add(statementItem);
                }

            }

            return statement;
        }
    }
}
