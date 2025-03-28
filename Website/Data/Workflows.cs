using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class WorkflowGroupGrandParent
    {
        [Key]
        public int ID { get; set; }
        public string WorkflowGroupGrandParentName { get; set; }
    }

    public class WorkflowGroupParent
    {
        [Key]
        public int ID { get; set; }
        public string WorkflowGroupParentName { get; set; }
        public int? WorkflowGroupGrandParentID { get; set; }
    }

    public class WorkflowGroup
    {
        [Key]
        public int ID { get; set; }
        public string WorkflowGroupName { get; set; }
        public int? BusinessDepartmentID { get; set; }
        public int? WorkflowGroupParentID { get; set; }
    }

    public class BusinessPillar
    {
        [Key]
        public int ID { get; set; }
        public string BusinessPillarName { get; set; }
    }

    public class BusinessDepartment
    {
        [Key]
        public int ID { get; set; }
        public string BusinessDepartmentName { get; set; }
        public int BusinessPillarID { get; set; }
    }
}
