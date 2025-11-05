using System.Text.Json.Serialization;

namespace Models.API;

public record GenericResponse<T> where T : class
{
	public string? Error { get; set; }
	public T? Result { get; set; }
	public bool Success => Result is not null;
	[JsonIgnore]
	public bool InternalServerError { get; set; } = false;

	public GenericResponse(string error) => Error = error;
}
