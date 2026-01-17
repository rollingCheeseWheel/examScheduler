using System.ComponentModel.DataAnnotations;

namespace Entities;

public class Classroom() : EntityBase<Classroom>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
    [Required]
    public required string Name { get; init; }
    [Required]
    public required School School { get; init; }
    public Guid SchoolId { get; }
    [Required]
    public required ICollection<int> RegisterId { get; set; } = [ ];
    [Required]
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    // Navigation Properties
    [Required]
    public Calendar? Calendar { get; set; }
    [Required]
    public ICollection<StudentProfile> Students { get; set; } = [ ];
    [Required]
    public ICollection<Teacher> Teachers { get; set; } = [ ];
    [Required]
    public ICollection<Schedule> Schedules { get; set; } = [ ];

    public override bool EqualsCore(Classroom b) =>
        Name == b.Name &&
        SchoolId == b.SchoolId;
    public override int GetHashCode() => HashCode.Combine(Name, SchoolId);
    public override int CompareTo(Classroom? b) => Name.CompareTo(b?.Name);
}
