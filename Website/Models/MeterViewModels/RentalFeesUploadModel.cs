using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class RentalFeesUploadModel
    {
        public string CSVString { get; set; }
        public string ErrorMessage { get; set; }
        public List<RentalUploadType> RentalUploadTypes
        {
            get
            {
                return new List<RentalUploadType>()
                {
                    new RentalUploadType() { ID=1, TypeName="Devices" },
                    new RentalUploadType() { ID=2, TypeName="Gateways" }
                };
            }
        }
        public int SelectedRentalUploadType { get; set; }
    }
    public class RentalUploadType
    {
        public int ID { get; set; }
        public string TypeName { get; set; }
    }
}
