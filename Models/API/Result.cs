using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

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
	public Result(T? data) => Data = data;
	public Result(T? data, HttpStatusCode errorCode = HttpStatusCode.BadRequest, HttpStatusCode successCode = HttpStatusCode.OK, Func<T?, bool>? isSuccess = null)
	{
		isSuccess ??= (d) => d is null;
		Data = data;
		StatusCode = isSuccess(data) ? successCode : errorCode;
	}
	public Result(T? data, IEnumerable<object>? errors, HttpStatusCode statusCode)
	{
		Data = data;
		Errors = errors;
		StatusCode = statusCode;
	}
	public Result(object error, HttpStatusCode errorCode = HttpStatusCode.BadRequest) : this([ error ], errorCode) { }
	public Result(IEnumerable<object> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
	{
		Errors = errors;
		StatusCode = statusCode;
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
