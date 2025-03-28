using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_SageManagementAccounts_ReportingCategoriesModel
    {
        public List<SiteAdmin_SageManagementAccounts_ReportingCategoriesItem> SiteAdmin_SageManagementAccounts_ReportingCategoriesItems { get; set; }
        public class SiteAdmin_SageManagementAccounts_ReportingCategoriesItem : Data.SageAccounting_AccountCategory
        {
        }

        public List<SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItem> SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItems { get; set; }
        public class SiteAdmin_SageManagementAccounts_ReportingParentDescriptionItem : Data.SageManagementAccounts_ReportingParentDescription
        {
        }

        public List<SiteAdmin_SageManagementAccounts_SageAccounting_Account> SiteAdmin_SageManagementAccounts_SageAccounting_Accounts { get; set; }
        public class SiteAdmin_SageManagementAccounts_SageAccounting_Account : Data.SageAccounting_Account
        {
            public string CompanyName { get; set; }
            public string CategoryName { get; set; }
            public string AccountTypeName { get; set; }
            public string TaxTypeName { get; set; }
        }
    }
}
