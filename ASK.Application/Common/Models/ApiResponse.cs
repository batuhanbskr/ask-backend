namespace ASK.Application.Common.Models;

/// <summary>
/// Tüm API yanıtları için standart zarf.
/// Frontend'deki ApiResponse<T> yapısıyla birebir uyumlu.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Sayfalandırılmış liste yanıtı.
/// Frontend'deki PaginatedResponse<T> ile uyumlu.
/// </summary>
public class PaginatedResponse<T> : ApiResponse<IReadOnlyList<T>>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages => Limit > 0 ? (int)Math.Ceiling((double)Total / Limit) : 0;

    public static PaginatedResponse<T> Ok(IReadOnlyList<T> data, int total, int page, int limit) =>
        new() { Success = true, Data = data, Total = total, Page = page, Limit = limit };
}
