using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.Customer_TariffModels
{
    public class Customer_TariffModel
    {
        public class Customer_TariffItem : MyVoltage.Api.SkyBill.TenantConsumptionStatementItem
        {
            public decimal UnitsBilled { get; set; }
            public decimal AmountBilled { get; set; }
            public class SkybillTariffItem : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public Data.SkybillResourceList SkybillResource { get; set; }
                public Data.SiteAdmin_Product Product { get; set; }
                public DateTime? ActivationDate { get; set; }
                public decimal? QuantityTo { get; set; }
                public decimal UnitsBilled { get; set; }
                public decimal AmountBilled { get; set; }
                public string RowClass { get; set; }
            }

            public List<SkybillTariffItem> SkybillTariffItems { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }

            public TarrifItem TarrifUsed { get; set; }
            public class TarrifItem : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public DateTime? End_Date { get; set; }
            }
        }

        public List<Customer_TariffItem> Customer_TariffItems { get; set; }
        public DateTime InvoiceMonth { get; set; }
        public bool ShowIncVAT { get; set; }
    }
}
