using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels
{
    public class CValidations
    {
        public class ValidatePhoneNumber : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                string valMessage = $"'{value}' - Invalid Phone Number";

                if (value == null)
                    return new ValidationResult(valMessage);

                // your validation logic
                if (value.ToString().Length == 10)
                {
                    try
                    {
                        Convert.ToInt32(value);
                        return ValidationResult.Success;
                    }
                    catch
                    {
                        return new ValidationResult(valMessage);
                    }
                }
                else
                {
                    return new ValidationResult(valMessage);
                }
            }
        }
    }
}
