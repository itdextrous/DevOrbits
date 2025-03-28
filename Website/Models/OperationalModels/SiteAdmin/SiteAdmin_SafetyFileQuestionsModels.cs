using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_SafetyFileQuestionsModels
{
    public class SiteAdmin_SafetyFileQuestionsModel
    {
        public List<SiteAdmin_SafetyFileQuestionsItem> SiteAdmin_SafetyFileQuestionsItems { get; set; }

        public class SiteAdmin_SafetyFileQuestionsItem : Data.SafetyFileQuestion
        {
            public string Username { get; set; }
        }
    }
    public class SiteAdmin_SafetyFileQuestionsAddModel
    {
        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Question Type")]
        public List<SelectListItem> QuestionTypeID { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureArea { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_SafetyFileQuestionsEditModel
    {
        public Data.SafetyFileQuestion SafetyFileQuestion { get; set; }
        public List<Data.SafetyFileQuestions_Company> SafetyFileQuestions_Companies { get; set; }

        [Required]
        [Display(Name = "Heading")]
        public string Heading { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Question Type")]
        public List<SelectListItem> QuestionTypeID { get; set; }

        [Required]
        [Display(Name = "Linked Secure Area")]
        public List<SelectListItem> LinkedSecureArea { get; set; }

        public bool IsSuccess { get; set; }
    }


}
