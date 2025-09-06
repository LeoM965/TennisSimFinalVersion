namespace TennisSim.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }

        public ValidationResult(bool isValid, string message = "")
        {
            IsValid = isValid;
            Message = message;
        }

        public static ValidationResult Valid => new ValidationResult(true);
    }
}
