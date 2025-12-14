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
	public HttpStatusCode StatusCode { get; set; }
	public HttpStatusCode ErrorCode { get; set; } = HttpStatusCode.BadRequest;

	public Result(HttpStatusCode statusCode) => StatusCode = statusCode;
	public Result(T? data, HttpStatusCode errorCode = HttpStatusCode.BadRequest, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		Data = data;
		StatusCode = statusCode;
		ErrorCode = errorCode;
	}
	public Result(object error, HttpStatusCode errorCode = HttpStatusCode.BadRequest) : this([ error ], errorCode) { }
	public Result(IEnumerable<object> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
	{
		Errors = errors;
		StatusCode = statusCode;
	}

	//public static implicit operator Result<T>(T? data)
	//{
	//	if (data is null)
	//	{
	//		return new(HttpStatusCode.BadRequest);
	//	}
	//	else
	//	{
	//		return new(data);
	//	}
	//}

	public async Task ExecuteResultAsync(ActionContext context)
	{
		var objectResult = new ObjectResult(this)
		{
			StatusCode = (int)( Success ? StatusCode : ErrorCode ),
			DeclaredType = typeof(Result<T>)
		};

		await objectResult.ExecuteResultAsync(context);
	}
}
