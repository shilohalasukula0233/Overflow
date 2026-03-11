using System.ComponentModel.DataAnnotations;

namespace Questionservice.Validators
{
    public class TagListValidator(int min,int max) : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is List<string> tags)
            {
                if (tags.Count >= min && tags.Count <= max) return ValidationResult.Success;
            }

            return new ValidationResult($"您至少需要提供{min}个最多{max}个标签");
        }
    }
}
