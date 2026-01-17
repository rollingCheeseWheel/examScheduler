using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Models.API;

public record Result<T> : IActionResult
{
    public IEnumerable<object>? Errors { get; set; }
    public T? Data { get; set; }
    public bool Success =>
        ( Errors is null || !Errors.Any() ) &&
        (int)StatusCode >= 200 && (int)StatusCode < 300;
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public Result(HttpStatusCode statusCode) => StatusCode = statusCode;
    public Result(T? data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Data = data;
        StatusCode = statusCode;
    }
    public Result(T? data, HttpStatusCode errorCode, Func<T?, bool>? isSuccess)
    {
        isSuccess ??= (data) => data is not null;
        Data = data;
        if (!isSuccess(data))
        {
            StatusCode = errorCode;
        }
    }
    public Result(T? data, HttpStatusCode errorCode, bool isSuccess)
    {
        Data = data;
        if (!isSuccess)
        {
            StatusCode = errorCode;
        }
    }
    public Result(HttpStatusCode errorCode, params object[ ] errors)
    {
        Errors = errors;
        StatusCode = errorCode;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var objectResult = new ObjectResult(this)
        {
            StatusCode = (int)StatusCode,
            DeclaredType = typeof(Result<T>)
        };

        await objectResult.ExecuteResultAsync(context);
    }
}
