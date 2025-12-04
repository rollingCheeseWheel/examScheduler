using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Models.API;

public record Result<T> : IActionResult
{
	public IEnumerable<object>? Errors { get; set; }
	public T? Data { get; set; }
	public bool Success => 
		(Errors is null || !Errors.Any()) &&
		(int)StatusCode >= 200 && (int)StatusCode < 300;

	[JsonIgnore]
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;

	public Result(HttpStatusCode statusCode) => StatusCode = statusCode;
	public Result(T? data, HttpStatusCode statusCode)
	{
		Data = data;
		StatusCode = statusCode;
	}
	public Result(T data) : this(data, HttpStatusCode.OK) { }
	public Result(T? data, HttpStatusCode errorStatusCode = HttpStatusCode.BadRequest, HttpStatusCode successStatusCode = HttpStatusCode.OK) : this(data, data is null ? errorStatusCode : successStatusCode) { }

	public Result(IEnumerable<object> errors, HttpStatusCode statusCode)
	{
		Errors = errors;
		StatusCode = statusCode;
	}
	public Result(IEnumerable<object> errors) : this(errors, HttpStatusCode.BadRequest) { }
	public Result(object error, HttpStatusCode statusCode) : this([ error ], statusCode) { }
	public Result(object error) : this(error, HttpStatusCode.BadRequest) { }

	public static implicit operator Result<T>(T? data)
	{
		if (data is null)
		{
			return new(HttpStatusCode.BadRequest);
		} else
		{
			return new(data);
		}
	}

	// Allow direct return from controller actions: return new Data<TSource>(...)
	public async Task ExecuteResultAsync(ActionContext context)
	{
		// Reuse MVC's normal formatter pipeline.
		var objectResult = new ObjectResult(this)
		{
			StatusCode = (int)StatusCode,
			DeclaredType = typeof(Result<T>)
		};

		await objectResult.ExecuteResultAsync(context);
	}

	public Result(string error) => Errors = [ error ];
}
