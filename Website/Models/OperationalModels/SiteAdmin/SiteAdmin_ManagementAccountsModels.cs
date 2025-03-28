using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_ManagementAccounts_ReportingCategoriesModel
    {
        public List<SiteAdmin_ManagementAccounts_ReportingCategoriesItem> SiteAdmin_ManagementAccounts_ReportingCategoriesItems { get; set; }
        public class SiteAdmin_ManagementAccounts_ReportingCategoriesItem : Data.ManagementAccounts_ReportingCategory
        {
        }

        public List<SiteAdmin_ManagementAccounts_ReportingDescriptionsItem> SiteAdmin_ManagementAccounts_ReportingDescriptionsItems { get; set; }
        public class SiteAdmin_ManagementAccounts_ReportingDescriptionsItem : Data.ManagementAccounts_ReportingDescription
        {
            public bool AllowDelete { get; set; }
            public string ProductNameToSync { get; set; }
        }

        public List<SiteAdmin_ManagementAccounts_ReportingParentDescriptionItem> SiteAdmin_ManagementAccounts_ReportingParentDescriptionItems { get; set; }
        public class SiteAdmin_ManagementAccounts_ReportingParentDescriptionItem : Data.ManagementAccounts_ReportingParentDescription
        {
        }
    }
}
