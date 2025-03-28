using System;
namespace MyVoltage.MyGasManager.Data
{
    public class AF_EDI_Order
    {
        public int ID { get; set; }
        public DateTime DatePosted { get; set; }
        public string UserID { get; set; }
        public int CompanyID { get; set; }
        public long AutoSupplyInsightsID { get; set; }
        public string ACOSupplyTransactionNo { get; set; }
        public int NumberOfCylinders { get; set; }
        public string PostXML { get; set; }
        public string ResponseXML { get; set; }
        public string ResponseStatusCode { get; set; }
        public string RefNum { get; set; }
    }
}
