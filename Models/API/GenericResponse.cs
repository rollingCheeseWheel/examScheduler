using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Models.API;

public record GenericResponse<T> : IActionResult
{
	public IEnumerable<object>? Errors { get; set; }
	public T? Result { get; set; }
	public bool Success => Result is not null && Errors is null;

	[JsonIgnore]
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;

	public GenericResponse(HttpStatusCode statusCode) => StatusCode = statusCode;
	public GenericResponse(T result, HttpStatusCode statusCode)
	{
		Result = result;
		StatusCode = statusCode;
	}
	public GenericResponse(T result) : this(result, HttpStatusCode.OK) { }
	public GenericResponse(IEnumerable<object> errors, HttpStatusCode statusCode)
	{
		Errors = errors;
		StatusCode = statusCode;
	}
	public GenericResponse(IEnumerable<object> errors) : this(errors, HttpStatusCode.BadRequest) { }
	public GenericResponse(object error, HttpStatusCode statusCode) : this([ error ], statusCode) { }
	public GenericResponse(object error) : this(error, HttpStatusCode.BadRequest) { }

	// Allow direct return from controller actions: return new GenericResponse<T>(...)
	public async Task ExecuteResultAsync(ActionContext context)
	{
		// Reuse MVC's normal formatter pipeline.
		var objectResult = new ObjectResult(this)
		{
			StatusCode = (int)StatusCode,
			DeclaredType = typeof(GenericResponse<T>)
		};

	public GenericResponse(string error) => Error = error;
}
