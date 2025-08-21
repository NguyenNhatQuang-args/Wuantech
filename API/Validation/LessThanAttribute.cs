using System.ComponentModel.DataAnnotations;

namespace WuanTech.API.Validation
{
    public class LessThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public LessThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (property == null)
                throw new ArgumentException($"Property {_comparisonProperty} not found");

            var comparisonValue = property.GetValue(validationContext.ObjectInstance);
            if (comparisonValue == null) return ValidationResult.Success;

            if (value is IComparable comparable && comparisonValue is IComparable comparableComparison)
            {
                if (comparable.CompareTo(comparableComparison) >= 0)
                {
                    return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be less than {_comparisonProperty}");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is DateTime dateTime)
            {
                if (dateTime <= DateTime.Now)
                {
                    return new ValidationResult(ErrorMessage ?? "Date must be in the future");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class PastDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is DateTime dateTime)
            {
                if (dateTime >= DateTime.Now)
                {
                    return new ValidationResult(ErrorMessage ?? "Date must be in the past");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is DateTime dateOfBirth)
            {
                var age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < _minimumAge)
                {
                    return new ValidationResult(ErrorMessage ?? $"Minimum age is {_minimumAge}");
                }
            }

            return ValidationResult.Success;
        }
    }
}
