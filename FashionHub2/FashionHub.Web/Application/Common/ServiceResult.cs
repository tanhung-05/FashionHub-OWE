namespace FashionHub.Web.Application.Common;

public enum ServiceErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

public sealed record ServiceError(
    ServiceErrorType Type,
    string Code,
    string Message);

public sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public T? Value { get; }

    public ServiceError? Error { get; }

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static ServiceResult<T> Failure(
        ServiceErrorType type,
        string code,
        string message)
    {
        return new(default, new ServiceError(type, code, message));
    }
}
