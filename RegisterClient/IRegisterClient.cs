using Entities;
using Models.DigitalesRegister;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IRegisterClient
{
	RegisterUserProfile? UserProfile { get; }

	Task<UserProfileRole?> GetRoleAsync(CancellationToken ct);

	Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct);

	Task<ICollection<RegisterClass>?> GetClassesAsync(CancellationToken ct);
	Task<ICollection<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct);

	Task<ICollection<Models.DigitalesRegister.CalendarDay>?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct);
	Task<ICollection<Models.DigitalesRegister.CalendarDay>> GetCalendarAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);
}
