using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net;
using System.Text.Json.Serialization;

namespace Models.API;

public record GenericResponse<T>
{
	public IEnumerable<object>? Errors { get; set; }
	public T? Result { get; set; }
	public bool Success => Result is not null;
	[JsonIgnore]
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;

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
}
