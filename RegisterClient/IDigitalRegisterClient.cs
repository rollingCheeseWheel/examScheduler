using Models.DigitalesRegister;
using Util;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IDigitalRegisterClient
{
	public const string ClientIdHeader = "API-CLIENT-ID";
	public const string ApiSecretHeader = "API-SECRET";
	public const string TokenHeader = "API-TOKEN";

	RegisterUserProfile? UserProfile { get; }
	long? UserId { get; }

	Task<bool> AuthenticateAsync(CancellationToken ct);

	Task<UserRoles?> GetRoleAsync(CancellationToken ct);

	Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct);

	Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct);
	Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct);

	Task<IEnumerable<Lesson>> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct);
	Task<IEnumerable<Lesson>> GetCalendarAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);

	static UserRoles? GetRole(RegisterUserProfile? userProfile) => userProfile?.Role switch
	{
		"student" => UserRoles.Student,
		"teacher" => UserRoles.Teacher,
		_ => null
	};
}
