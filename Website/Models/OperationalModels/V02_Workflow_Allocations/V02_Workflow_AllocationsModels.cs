using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.V02_Workflow_Allocations.V02_Workflow_AllocationsModels
{
    public class V02_Workflow_Allocations_AllTaskAllocationsModel
    {
        [Display(Name = "Workflow Group")]
        public List<SelectListItem> SecureAreaGroupID { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Reporting To User")]
        public List<SelectListItem> ReportingToUser { get; set; }

        [Display(Name = "Priority")]
        public List<SelectListItem> Priority { get; set; }

        public List<V02_Workflow_Allocations_AllTaskAllocationsItem> V02_Workflow_Allocations_AllTaskAllocationsItems { get; set; }
        public class V02_Workflow_Allocations_AllTaskAllocationsItem : Data.A08_Task_Type
        {
            public string ResponsibleUsername { get; set; }
            public string ReportingToUsername { get; set; }
            public string SecureAreaName { get; set; }
            public string WorkflowGroupName { get; set; }
            public string BusinessDepartmentName { get; set; }
            public string BusinessPillarName { get; set; }
        }
    }

    public class V02_Workflow_Allocations_ViewTaskAllocationsModel
    {
        public V02_Workflow_AllocationsItem A08_Task_Type { get; set; }
        public class V02_Workflow_AllocationsItem : Data.A08_Task_Type
        {
            public string ResponsibleUsername { get; set; }
            public string ReportingToUsername { get; set; }
            public string SecureAreaName { get; set; }
            public string WorkflowGroupName { get; set; }
            public string BusinessDepartmentName { get; set; }
            public string BusinessPillarName { get; set; }
        }

    }


}
