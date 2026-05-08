namespace Httpflow.Api.Infrastructure.Exceptions;

public sealed class UnauthorizedApiException : Exception
{
    public UnauthorizedApiException(string message) : base(message)
    {
    }
}
