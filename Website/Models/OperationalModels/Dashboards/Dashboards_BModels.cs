using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Dashboards.Dashboards_BModels
{
    public class B1_011_Account_Details_SummaryModel : B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel { }
    public class B1_021_Supply_Cost_Settings_SummaryModel : B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel { }
    public class B2_012_Council_Reading_SummaryModel : B02_CouncilReadingsModels.B02_CouncilReadings_CouncilReadingSummaryModel { }
    public class B3_011_Council_Statement_SummaryModel : B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel { }
    public class B5_011_Payment_SummaryModel : B05_SupplyPayments.B05_SupplyPaymentsModels.B05_AccountPayments_PaymentSummaryModel { }
}
