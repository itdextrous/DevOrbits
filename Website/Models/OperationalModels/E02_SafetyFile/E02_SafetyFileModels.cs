using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.E02_SafetyFileModels
{
    public class E02_SafetyFile_AnswersModel
    {
        public List<E02_SafetyFile_AnswersItem> E02_SafetyFile_AnswersItems { get; set; }

        public class E02_SafetyFile_AnswersItem : Data.SafetyFileQuestion
        {
            public string Username { get; set; }
            public Answer Answered { get; set; }
            public class Answer : Data.SafetyFileAnswer
            {
                public string Username { get; set; }
            }
        }
    }

    public class E02_SafetyFile_Answers_Update_TXTModel
    {
        public Data.SafetyFileQuestion SafetyFileQuestion { get; set; }

        [Required]
        [Display(Name = "Answer")]
        public string Answer { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class E02_SafetyFile_Answers_Update_FUModel
    {
        public Data.SafetyFileQuestion SafetyFileQuestion { get; set; }

        [Required]
        [Display(Name = "Describe the file")]
        public string Answer { get; set; }

        [Required]
        [Display(Name = "Browse a file to attach")]
        public IFormFile Attachment { get; set; }


        public bool IsSuccess { get; set; }
    }

    public class E02_SafetyFile_Answers_Update_YNModel
    {
        public Data.SafetyFileQuestion SafetyFileQuestion { get; set; }

        [Required]
        [Display(Name = "Explain your answer")]
        public string Answer { get; set; }

        [Display(Name = "Answer")]
        public List<SelectListItem> QuestionTypeAnswer { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class E02_SafetyFile_LogsModel
    {
        public List<E02_SafetyFile_LogsItem> E02_SafetyFile_LogsItems { get; set; }

        public class E02_SafetyFile_LogsItem : Data.SafetyFileLog
        {
            public string Username { get; set; }
            public Data.SafetyFileQuestion SafetyFileQuestion { get; set; }
        }
    }

}
