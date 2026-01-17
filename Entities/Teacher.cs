using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Teacher : EntityBase<Teacher>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
    [Required]
    public required int RegisterID { get; init; }
    [Required]
    public required string FirstName { get; init; }
    [Required]
    public required string LastName { get; init; }
    [NotMapped]
    public string Name => string.Join(" ", FirstName, LastName);
    [Required]
    public required School School { get; init; }
    public Guid SchoolId { get; init; }

    public TeacherProfile? TeacherProfile { get; set; }
    [Required]
    public ICollection<Subject> Subjects { get; set; } = [ ];
    [Required]
    public ICollection<Classroom> Classrooms { get; set; } = [ ];
    [Required]
    public ICollection<Lesson> Lessons { get; set; } = [ ];

    public override bool EqualsCore(Teacher b) =>
        Name == b.Name &&
        SchoolId == b.SchoolId &&
        Subjects.ValueEquals(b.Subjects) &&
        Classrooms.ValueEquals(b.Classrooms);
    public override int GetHashCode() => HashCode.Combine(Name, SchoolId, Subjects.Order(), Classrooms.Order());
    public override int CompareTo(Teacher? b) => Name.CompareTo(b?.Name);
}