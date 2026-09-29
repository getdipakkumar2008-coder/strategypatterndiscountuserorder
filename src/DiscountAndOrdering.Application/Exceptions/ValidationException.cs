namespace DiscountAndOrdering.Application.Exceptions;

/// <summary>Raised for request-level validation failures the controller maps to 400.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
