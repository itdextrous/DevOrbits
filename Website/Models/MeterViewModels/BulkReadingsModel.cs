using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Models.MeterViewModels
{
    public class BulkReadingsModel
    {
        public string SerialNos { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime EndTime { get; set; }
        public int Interval { get; set; }
        public List<BulkReadingsRegister> Registers { get; set; }
        public string ErrorMessage { get; set; }
        public string SelectedType { get; set; }
    }
    public class BulkReadingsRegister
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<string> Type
        {
            get
            {
                return new List<string>()
                {
                    "register",
                    "diff"
                };
            }
        }
        public string SelectedType { get; set; }
        public bool Selected { get; set; }
    }
}
