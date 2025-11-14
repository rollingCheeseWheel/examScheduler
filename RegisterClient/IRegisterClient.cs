using Models.DigitalesRegister;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IRegisterClient
{
	public RegisterUserProfile? UserProfile { get; }
	public RegisterUserProfileRole? Role { get; }

	Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct);

	Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct);
	Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct);

	Task<IEnumerable<CalendarDay>?> GetUpcomingCalendar(CancellationToken ct);
}
