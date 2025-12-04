using Entities;
using Models.DigitalesRegister;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IRegisterClient
{
	RegisterUserProfile? UserProfile { get; }

	Task<UserProfileRole?> GetRoleAsync(CancellationToken ct);

	Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct);

	Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct);
	Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct);

	Task<IEnumerable<Models.DigitalesRegister.Lesson>?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct);
	Task<IEnumerable<Models.DigitalesRegister.Lesson>> GetCalendarAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);
}
