using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.ComponentModel;

namespace MyVoltage.Api.Prism
{
    public class PrismVendClient
    {
        #region XML Response Classes

        public class QueryTx
        {
            // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
            /// <remarks/>
            [System.SerializableAttribute()]
            [System.ComponentModel.DesignerCategoryAttribute("code")]
            [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
            [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
            public partial class response
            {

                private int txCreditField;

                /// <remarks/>
                public int txCredit
                {
                    get
                    {
                        return this.txCreditField;
                    }
                    set
                    {
                        this.txCreditField = value;
                    }
                }
            }
        }

        public class VendMse
        {

            // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
            /// <remarks/>
            [System.SerializableAttribute()]
            [System.ComponentModel.DesignerCategoryAttribute("code")]
            [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
            [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
            public partial class response
            {

                private string idRecordField;

                private int subclassField;

                private string descriptionField;

                private uint vendTimeUnixField;

                private decimal unitsActualField;

                private string unitNameField;

                private string tokenHexField;

                private string tokenDecField;

                /// <remarks/>
                [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
                public string idRecord
                {
                    get
                    {
                        return this.idRecordField;
                    }
                    set
                    {
                        this.idRecordField = value;
                    }
                }

                /// <remarks/>
                public int subclass
                {
                    get
                    {
                        return this.subclassField;
                    }
                    set
                    {
                        this.subclassField = value;
                    }
                }

                /// <remarks/>
                public string description
                {
                    get
                    {
                        return this.descriptionField;
                    }
                    set
                    {
                        this.descriptionField = value;
                    }
                }

                /// <remarks/>
                public uint vendTimeUnix
                {
                    get
                    {
                        return this.vendTimeUnixField;
                    }
                    set
                    {
                        this.vendTimeUnixField = value;
                    }
                }

                /// <remarks/>
                public decimal unitsActual
                {
                    get
                    {
                        return this.unitsActualField;
                    }
                    set
                    {
                        this.unitsActualField = value;
                    }
                }

                /// <remarks/>
                public string unitName
                {
                    get
                    {
                        return this.unitNameField;
                    }
                    set
                    {
                        this.unitNameField = value;
                    }
                }

                /// <remarks/>
                public string tokenHex
                {
                    get
                    {
                        return this.tokenHexField;
                    }
                    set
                    {
                        this.tokenHexField = value;
                    }
                }

                /// <remarks/>
                public string tokenDec
                {
                    get
                    {
                        return this.tokenDecField;
                    }
                    set
                    {
                        this.tokenDecField = value;
                    }
                }
            }


        }

        #endregion

        #region Class Declaration

        public string _prodURL = "http://mvprismddns.ddns.net:8080/stsvend/";
        //private string _prodURL = "http://165.255.251.95:8080/stsvend/";
        public string _backupURL = "http://169.0.86.122:8080/stsvend/";
        private readonly DbContextOptions<MyVoltageDbContext> _options;

        #endregion

        public PrismVendClient(DbContextOptions<MyVoltageDbContext> options)
        {
            _options = options;
        }

        public QueryTx.response GetRemainingCredit(bool prod = true)
        {
            try
            {
                string URI = $"{(prod ? _prodURL : _backupURL)}QueryTx.xml";
                string myParameters = "";

                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.DownloadString(URI);
                    return HtmlResult.ToObject<QueryTx.response>();
                }
            }
            catch
            {
                return null;
            }
        }

        public string VendMeterSpecificEngineeringToken(VendMseSubclass subclass, string meterSerial, decimal value, string source, decimal? balance, string contactorState, string userID)
        {
            try
            {
                string URI = $"{(_prodURL)}VendMse.xml";
                string myParameters = $"subclass={((int)subclass)}&meterId={meterSerial}&value={value}";

                Log_TokenGeneration log_TokenGenerations = new Log_TokenGeneration()
                {
                    URL = URI,
                    SerialNo = meterSerial,
                    SGC = "",
                    DateRequested = DateTime.Now,
                    Source = source,
                    Balance = balance,
                    ContactorState = contactorState,
                    UserID = userID,
                    Type = subclass.GetDescription(),
                };

                log_TokenGenerations.RequestXML = myParameters;

                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                log_TokenGenerations.DateCompleted = DateTime.Now;

                db.Log_TokenGenerations.Add(log_TokenGenerations);
                db.SaveChanges();


                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string HtmlResult = wc.UploadString(URI, myParameters);

                        var result = HtmlResult.ToObject<VendMse.response>();

                        log_TokenGenerations.DateCompleted = DateTime.Now;
                        log_TokenGenerations.ResponseXML = HtmlResult;

                        if (result != null)
                        {
                            log_TokenGenerations.Token = result.tokenDec;
                        }

                        db.Log_TokenGenerations.Update(log_TokenGenerations);
                        db.SaveChanges();

                        return result.tokenDec;
                    }
                }
                catch
                {
                    URI = $"{(_backupURL)}VendMse.xml";
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string HtmlResult = wc.UploadString(URI, myParameters);

                        var result = HtmlResult.ToObject<VendMse.response>();

                        log_TokenGenerations.URL = URI;
                        log_TokenGenerations.DateCompleted = DateTime.Now;
                        log_TokenGenerations.ResponseXML = HtmlResult;

                        if (result != null)
                        {
                            log_TokenGenerations.Token = result.tokenDec;
                        }

                        db.Log_TokenGenerations.Update(log_TokenGenerations);
                        db.SaveChanges();

                        return result.tokenDec;
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        public enum VendMseSubclass
        {
            [Description("SetMaximumPowerLimit 0 to 18,201,624")]
            SetMaximumPowerLimit = 0,
            [Description("ClearCredit 0 to 65535")]
            ClearCredit = 1,
            [Description("ClearTamperCondition 0")]
            ClearTamperCondition = 5,
            [Description("SetMaximumPhasePowerUnbalanceLimit 0 to 18,201,624")]
            SetMaximumPhasePowerUnbalanceLimit = 6,
            [Description("SetWaterMeterFactor 0 to 65535")]
            SetWaterMeterFactor = 7,

            [Description("Switch to Postpaid")]
            SetPostpaid = 8,
            [Description("Switch to Prepaid")]
            SetPrepaid = 9,
        }
    }
}
