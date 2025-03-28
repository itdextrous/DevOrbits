using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_Imports_Models
{
    public class SiteAdmin_Imports_RentalDataDumpModel
    {
        [Display(Name = "Upload File (.xlsx)")]
        [Required]
        public IFormFile UploadFile { get; set; }

        public bool IsSuccessfull { get; set; }
        public string ResultMessage { get; set; }

        public List<SiteAdmin_Imports_RentalDataDumpItem> SiteAdmin_Imports_RentalDataDumpItems { get; set; }

        public class SiteAdmin_Imports_RentalDataDumpItem : Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalDataDump
        {
            public string Username { get; set; }
        }
    }
    public class SiteAdmin_Imports_RentalExpensesModel
    {
        [Display(Name = "Upload File (.xlsx)")]
        [Required]
        public IFormFile UploadFile { get; set; }

        public bool IsSuccessfull { get; set; }
        public string ResultMessage { get; set; }

        public List<SiteAdmin_Imports_RentalExpensesItem> SiteAdmin_Imports_RentalExpensesItems { get; set; }

        public class SiteAdmin_Imports_RentalExpensesItem : Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalExpense
        {
            public string Username { get; set; }
        }
    }
    public class SiteAdmin_Imports_ManagementAccountsDataDumpModel
    {
        [Display(Name = "Upload File (.xlsx)")]
        [Required]
        public IFormFile UploadFile { get; set; }

        public bool IsSuccessfull { get; set; }
        public string ResultMessage { get; set; }

        public List<SiteAdmin_Imports_ManagementAccountsDataDumpItem> SiteAdmin_Imports_ManagementAccountsDataDumpItems { get; set; }

        public class SiteAdmin_Imports_ManagementAccountsDataDumpItem : Data.SiteAdmin_Imports.SiteAdmin_Imports_ManagementAccountsDataDump
        {
            public string Username { get; set; }
        }
    }
    public class SiteAdmin_Imports_SuburbsModel
    {
        [Display(Name = "Upload File (.xlsx)")]
        [Required]
        public IFormFile UploadFile { get; set; }

        public bool IsSuccessfull { get; set; }
        public string ResultMessage { get; set; }

        public List<SiteAdmin_Imports_SuburbsItem> SiteAdmin_Imports_SuburbsItems { get; set; }

        public class SiteAdmin_Imports_SuburbsItem : Data.SiteAdmin_Imports.SiteAdmin_Imports_Suburb
        {
            public string Username { get; set; }
        }
    }
}
