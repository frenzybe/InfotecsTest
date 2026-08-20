namespace WebApi.Common;

public class Result
{
    public bool IsSuccess { get;}
    public string? ErrorMessage { get; }
    public bool IsFailure => !IsSuccess;
    
    protected Result(bool isSuccess, string? message)
    {
        IsSuccess = isSuccess;
        ErrorMessage = message;
    }
    
    public static Result Success() => new Result(true, null);
    
    public static Result Failure(string error) => new Result(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    
    public Result(bool isSuccess, T? value, string? errorMessage) : base(isSuccess, errorMessage)
    {
        Value = value;
    }
    
    public static Result<T> Success(T value) => new Result<T>(true, value,  null);
    public new static Result<T> Failure(string error) => new(false, default, error);
    
}