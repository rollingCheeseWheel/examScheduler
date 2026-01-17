using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util.Converters;

namespace Models.DigitalesRegister;

public class RegisterUserProfile
{
    /// <summary>
    /// Maps to UserProfile.Username
    /// </summary>
    [Required]
    public required int Id { get; set; }
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    [Required]
    public required string Role { get; set; }

    public string? Picture { get; set; }
    public StudentData? StudentData { get; set; }
}

public class StudentData
{
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public required string JoinedName { get; set; }
    [JsonPropertyName("firstName")]
    public required string Name { get; set; }
    [JsonPropertyName("lastName")]
    public required string Surname { get; set; }
    [JsonPropertyName("mainclass")]
    public required StudenProfileClass? MainClass { get; set; }
    [JsonPropertyName("classes")]
    public required StudenProfileClass[ ] OtherClasses { get; set; } = [ ];
}

public class StudenProfileClass
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [JsonConverter(typeof(IntToBoolConverter))]
    public bool ChoiceSubject { get; set; } = false;
    [JsonConverter(typeof(IntToBoolConverter))]
    public bool BelongsTo { get; set; } = false;
}