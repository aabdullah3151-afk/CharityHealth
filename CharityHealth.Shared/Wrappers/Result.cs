namespace CharityHealth.Shared.Wrappers;

public class Result<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = [];

    public static Result<T> Success(T data, string? message = null) =>
        new() { Succeeded = true, Data = data, Message = message };

    public static Result<T> Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };

    public static Result<T> Failure(List<string> errors) =>
        new() { Succeeded = false, Errors = errors };
}

public class Result
{
    public bool Succeeded { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = [];

    public static Result Success(string? message = null) =>
        new() { Succeeded = true, Message = message };

    public static Result Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
