namespace Httpflow.Api.Infrastructure.Exceptions;

public sealed class ForbiddenApiException : Exception
{
    public ForbiddenApiException(string message) : base(message)
    {
    }
}
