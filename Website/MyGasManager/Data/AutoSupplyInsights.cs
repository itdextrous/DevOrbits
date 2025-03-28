using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.MyGasManager.Data
{
    public class AutoSupplyInsights
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string ACOSupplyTransactionNo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
        public string CompanyName { get; set; }
        public string ACOServiceCylinderBank { get; set; }
        public DateTime ActivationDate { get; set; }
        public int NumberOfCylinders { get; set; }
        public int CapacityPerCylinder { get; set; }
        public int TotalSupplyCapacity { get; set; }
        public string ACOReserveCylinderBank { get; set; }
        public string ACOReserveNumber { get; set; }
        public string MeteringPoint { get; set; }
        public string MeterSerialNo { get; set; }
        public DateTime? DepletionDate { get; set; }
        public bool TamperDetection { get; set; }
        public bool SupplyStatus { get; set; }
    }

    class DistinctSupplyComparer : IEqualityComparer<AutoSupplyInsights>
    {

        public bool Equals(AutoSupplyInsights x, AutoSupplyInsights y)
        {
            return x.ACOSupplyTransactionNo == y.ACOSupplyTransactionNo;
        }

        public int GetHashCode(AutoSupplyInsights obj)
        {
            return obj.ACOSupplyTransactionNo.GetHashCode();
        }
    }

    public class ACO_Status
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Status { get; set; }
    }

    public class AutoSupplyInsights_Log
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public long AutoSupplyInsightsID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }


}
