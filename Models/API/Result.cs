using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json.Serialization;

namespace Models.API;

public record Result : IActionResult
{
	public string[ ]? Errors { get; set; }

	public bool Success =>
		( Errors is null || Errors.Length == 0 ) &&
		(int)StatusCode >= 200 && (int)StatusCode < 300;

	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

	public Result(HttpStatusCode statusCode)
	{
		StatusCode = statusCode;
	}

	public Result(HttpStatusCode errorCode, params string[ ] errors)
	{
		Errors = errors;
		StatusCode = errorCode;
	}

	protected Result(HttpStatusCode statusCode, IEnumerable<string>? errors)
	{
		Errors = errors?.ToArray();
		StatusCode = statusCode;
	}

	public async Task ExecuteResultAsync(ActionContext context)
	{
		var objectResult = new ObjectResult(this)
		{
			StatusCode = (int)StatusCode,
			DeclaredType = typeof(Result)
		};

		await objectResult.ExecuteResultAsync(context);
	}

	public Result<T> To<T>(T? data = default)
	{
		return new(data, StatusCode, Errors);
	}

	public Result MergeErrors(params Result[ ] others)
	{
		var errors = others
			.SelectMany(r => r.Errors ?? [ ])
			.Concat(Errors ?? [ ])
			.ToList();

		if (errors.Count != 0)
		{
			return new(HttpStatusCode.BadRequest, errors);
		}
		return this;
	}
}

public record Result<T> : Result
{
	public T? Data { get; set; }

	public Result(HttpStatusCode statusCode)
		: base(statusCode)
	{
	}

	public Result(T? data, HttpStatusCode statusCode = HttpStatusCode.OK)
		: base(statusCode)
	{
		Data = data;
	}

	public Result(T? data, HttpStatusCode errorCode, Func<T?, bool>? isSuccess)
		: base(HttpStatusCode.OK)
	{
		isSuccess ??= d => d is not null;

		Data = data;

		if (!isSuccess(data))
		{
			StatusCode = errorCode;
		}
	}

	public Result(T? data, HttpStatusCode errorCode, bool isSuccess)
		: base(HttpStatusCode.OK)
	{
		Data = data;

		if (!isSuccess)
		{
			StatusCode = errorCode;
		}
	}

	public Result(HttpStatusCode errorCode, params string[ ] errors)
		: base(errorCode, errors)
	{
	}

	public Result(T? data, HttpStatusCode statusCode, IEnumerable<string>? errors) : base(statusCode, errors)
	{
		Data = data;
	}

	public new async Task ExecuteResultAsync(ActionContext context)
	{
		var objectResult = new ObjectResult(this)
		{
			StatusCode = (int)StatusCode,
			DeclaredType = typeof(Result<T>)
		};

		await objectResult.ExecuteResultAsync(context);
	}
}