using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class TeacherProfileClassroom
{
    [Required]
    public required Guid Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required Guid SchoolId { get; set; }
    public required Guid? CalendarId { get; set; }
}

public class Classroom : TeacherProfileClassroom
{
    public required Calendar? Calendar { get; set; }
}