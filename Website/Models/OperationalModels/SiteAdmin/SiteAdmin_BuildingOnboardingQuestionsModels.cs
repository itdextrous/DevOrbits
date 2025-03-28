using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_BuildingOnboardingQuestionsModels
{
    public class SiteAdmin_BuildingOnboardingQuestionsModel
    {
        public List<SiteAdmin_BuildingOnboardingQuestionsItem> SiteAdmin_BuildingOnboardingQuestionsItems { get; set; }

        public class SiteAdmin_BuildingOnboardingQuestionsItem : Data.BuildingOnboardingQuestion
        {
            public string Username { get; set; }
        }
    }
    public class SiteAdmin_BuildingOnboardingQuestionsAddModel
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

    public class SiteAdmin_BuildingOnboardingQuestionsEditModel
    {
        public Data.BuildingOnboardingQuestion BuildingOnboardingQuestion { get; set; }
        public List<Data.BuildingOnboardingQuestions_Company> BuildingOnboardingQuestions_Companies { get; set; }

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
