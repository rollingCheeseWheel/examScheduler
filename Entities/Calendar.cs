using System.ComponentModel.DataAnnotations;
using Util;

namespace Entities;

public class Calendar : EntityBase<Calendar>
{
    [Key]
    public override Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateTimeOffset LastsUntil { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public ICollection<Lesson> Lessons { get; set; } = [ ];
    //[Required]
    //public required Classroom Classroom { get; set; }
    //public Guid ClassroomId { get; }

    public void Extend(IEnumerable<Models.DigitalesRegister.Lesson> lessons, School school, out IEnumerable<Teacher> createdTeachers, out IEnumerable<Subject> createdSubjects)
    {
        var existingTeacherSubjects = Lessons
            .GroupBy(l => l.Subject)
            .Select(g => (
                Subject: g.Key,
                Teachers: g.SelectMany(l => l.Teachers).Distinct(),
                Lessons: (IEnumerable<Models.DigitalesRegister.Lesson>)[ ]
            ));

        var registerTeacherSubjects = lessons
            .GroupBy(l => l.Subject)
            .Select(g => (
                Subject: new Subject()
                {
                    Name = g.Key.Name,
                    RegisterId = g.Key.Id
                },
                Teachers: g.SelectMany(l => l.Teachers).Distinct()
                .Select(t => new Teacher()
                {
                    FirstName = t.FirstName,
                    LastName = t.LastName,
                    RegisterID = t.Id,
                    School = school,
                }),
                Lessons: g.Select(l => l)
            ));

        var additionalTeacherSubjects = registerTeacherSubjects.Except(existingTeacherSubjects, g => g.Subject);

        createdSubjects = additionalTeacherSubjects.Select(g => g.Subject);
        createdTeachers = additionalTeacherSubjects.SelectMany(g => g.Teachers);

        var combinedTeacherSubjects = existingTeacherSubjects
            .Concat(additionalTeacherSubjects)
            .GroupBy(g => g.Subject)
            .Select(g => (
                Subject: g.Key,
                Teachers: g.SelectMany(x => x.Teachers),
                Lessons: g.SelectMany(x => x.Lessons)
            ));

        foreach (var iterLesson in lessons)
        {
            var matchingTeacherSubjects = combinedTeacherSubjects.FirstOrDefault(g => g.Lessons.Contains(iterLesson));
            var existingLesson = Lessons
                .Where(l => l.EqualsModel(iterLesson))
                .FirstOrDefault();
            if (existingLesson is null)
            {
                existingLesson = new()
                {
                    LessonId = iterLesson.LessonId,
                    LessonName = iterLesson.LessonName,
                    FromHour = Math.Clamp(iterLesson.FromHour - 1, 0, 23),
                    ToHour = Math.Clamp(iterLesson.ToHour - 1, 0, 23),
                    Occurances = [ iterLesson.Date ],
                    Subject = matchingTeacherSubjects.Subject,
                    Teachers = matchingTeacherSubjects.Teachers.ToList(),
                };
                Lessons.Add(existingLesson);
            }
            else
            {
                existingLesson.Occurances.Add(iterLesson.Date);
                existingLesson.Teachers.Clear();
                foreach (var teacher in matchingTeacherSubjects.Teachers)
                {
                    existingLesson.Teachers.Add(teacher);
                }
            }
        }
    }

    public IEnumerable<Lesson> Normalize()
    {
        var result = new List<Lesson>();

        var daysInWeek = Enum.GetValues<DayOfWeek>();
        var longestDayInWeek = Lessons
            .GroupBy(l => l.DayOfWeek)
            .Max(g => g.Select(l => l.FromHour + l.Duration).Max());
        var lessonMatrix = new Lesson?[ daysInWeek.Length, longestDayInWeek ];

        for (var day = 0; day < daysInWeek.Length; day++)
        {
            for (var hour = 0; hour < longestDayInWeek; hour++)
            {
                lessonMatrix[ day, hour ] = Lessons
                    .Where(l
                        => l.DayOfWeek == daysInWeek[ day ]
                        && l.FromHour <= hour
                        && l.ToHour >= hour
                    )
                    .MaxBy(l => l.Occurances.Count);
            }

            for (var hour = 0; hour < longestDayInWeek; hour++)
            {
                var lesson = lessonMatrix.GetOrDefault(day, hour);
                if (lesson is null) { continue; }

                for (var fromHour = lesson.FromHour; fromHour < lesson.FromHour + lesson.Duration; fromHour++)
                {
                    var valueToOverride = lessonMatrix.GetOrDefault(day, fromHour);
                    if (valueToOverride is not null && valueToOverride.Occurances.Count > lesson.Occurances.Count)
                    {
                        continue;
                    }

                    var replacement = new Lesson
                    {
                        FromHour = fromHour,
                        ToHour = lesson.ToHour,

                        LessonId = lesson.LessonId,
                        LessonName = lesson.LessonName,
                        Occurances = lesson.Occurances,
                        Subject = lesson.Subject,
                        Teachers = lesson.Teachers,
                    };
                    lessonMatrix.TrySet(day, fromHour, replacement);
                }
            }

            Lesson? cursor = null;
            var tempResult = new List<Lesson>();
            for (var hour = 0; hour < longestDayInWeek; hour++)
            {
                var lesson = lessonMatrix.GetOrDefault(day, hour);
                if (lesson is null) { continue; }
                if (cursor is null || !cursor.ShallowEqual(lesson))
                {
                    cursor = lesson;
                    tempResult.Add(lesson);
                }
                else
                {
                    cursor = new()
                    {
                        FromHour = cursor.FromHour,
                        ToHour = lesson.ToHour,


                        LessonId = cursor.LessonId,
                        LessonName = cursor.LessonName,
                        Occurances = cursor.Occurances,
                        Subject = cursor.Subject,
                        Teachers = cursor.Teachers,
                    };
                    tempResult[ ^1 ] = cursor;
                }
            }
            result.AddRange(tempResult);
        }

        return result;
    }

    public override bool EqualsCore(Calendar other) =>
        //Classroom == other.Classroom &&
        Lessons.ValueEquals(other.Lessons);
    public override int GetHashCode() => HashCode.Combine(/*Classroom,*/ Lessons.Order());
    //public override int CompareTo(Calendar? b) => Classroom.CompareTo(b?.Classroom);
}
