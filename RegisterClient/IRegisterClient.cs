using Models.DigitalesRegister;
using Util;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IRegisterClient : IDisposable
{
	RegisterUserProfile? UserProfile { get; }

	Task<UserRole?> GetRoleAsync(CancellationToken ct);

	Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct);

	Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct);
	Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct);

	Task<IEnumerable<Models.DigitalesRegister.Lesson>?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct);
	Task<IEnumerable<Models.DigitalesRegister.Lesson>> GetCalendarAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);

	IRegisterClient Copy();
}
