using System.Net;
using System.Text.Json.Serialization;

namespace Models.API;

public record GenericResponse<T> where T : class
{
	public string? Error { get; set; }
	public T? Result { get; set; }
	public bool Success => Result is not null;
	[JsonIgnore]
	public HttpStatusCode InternalServerError { get; set; } = HttpStatusCode.BadRequest;

	public GenericResponse(string error) => Error = error;
	public GenericResponse(T? result) => Result = result;
}
