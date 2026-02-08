using Microsoft.AspNetCore.WebUtilities;
using Models.DigitalesRegister;
using System.Text;
using System.Text.Json;
using Util;
using Util.DataStructures;
using Util.Extensions;

namespace registerClient;

/// <summary>
/// Cannot really make use of parallelisation since the API limits connections are throttled to 1 second after a certain delay
/// </summary>
public class DigitalRegisterClient : IDigitalRegisterClient, IDisposable
{
	public readonly Uri SchoolUri;
	public readonly string ClientId;
	private readonly HttpClient _httpClient;
	private readonly HttpClientHandler _clientHandler;
	private readonly RequestThrottler _requestThrottler;
	private readonly double _targetRequestsPerSecond;

	private readonly string _authCode;
	private readonly string _secret;
	private string? _accessToken;
	private string? _refreshToken;
	public DateTimeOffset? TokenExpiration { get; private set; }
	public long? UserId { get; private set; }

	private readonly SemaphoreSlim _authenticationSemaphore = new(1, 1);

	public RegisterUserProfile? UserProfile { get; private set; }

	public DigitalRegisterClient(Entities.School school, string authCode, double targetRequestsPerSecond = 1) : this(school.RegisterUri, school.ClientId, school.Secret, authCode, targetRequestsPerSecond) { }

	public DigitalRegisterClient(Uri schoolUri, string clientId, string secret, string authCode, double targetRequestsPerSecond = 1)
	{
		SchoolUri = schoolUri.GetSchemeAndAuthority();
		ClientId = clientId;
		_authCode = authCode;
		_secret = secret;

		_clientHandler = new()
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
			{
				return errors == System.Net.Security.SslPolicyErrors.None && ( message.RequestUri?.Scheme ) == Uri.UriSchemeHttps;
			}
		};
		_httpClient = new(_clientHandler);
		_requestThrottler = new(targetRequestsPerSecond);
		_targetRequestsPerSecond = targetRequestsPerSecond;
	}

	public async Task<UserRoles?> GetRoleAsync(CancellationToken ct = default)
	{
		UserProfile ??= await GetUserProfileAsync(ct);
		return IDigitalRegisterClient.GetRole(UserProfile);
	}

	public async Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct = default)
	{
		UserProfile = await GetAsync<RegisterUserProfile>(RegisterPathAPI.UserProfile, ct: ct);
		return UserProfile;
	}

	public async Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct = default) => await GetAsync<ICollection<RegisterClass>>(RegisterPathAPI.Classes, ct: ct);

	public async Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct = default) => await GetAsync<ICollection<RegisterSubject>>(RegisterPathAPI.Subjects, ct);

	/// <summary>
	/// The calendar is only available for a couple of weeks after the start date
	/// </summary>

	public async Task<IEnumerable<Lesson>> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct = default)
	{
		var args = new Dictionary<string, string> { { "startDate", date.RoundDownToMonday().ToRegisterFormat() } };

		var response = await GetAsync(RegisterPathAPI.LessonWeek, args, ct);
		if (response is null)
		{
			return [ ];
		}

		try
		{
			return ParseCalendarDays(JsonDocument.Parse(await response.ReadContentAsStringAsync(ct))) ?? [ ];
		}
		catch
		{
			return [ ];
		}
	}

	// TODO there is a bug where the same data gets outputted multiple times
	public async Task<IEnumerable<Lesson>> GetCalendarAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default)
	{
		if (!await AuthenticateAsync(ct)) { return [ ]; }

		var iterdate = startDate;
		var dates = new List<DateTimeOffset>();
		while (iterdate < endDate)
		{
			dates.Add(iterdate);
			iterdate = iterdate.AddDays(7);
		}

		Console.WriteLine(dates.Stringify());

		var tasks = new List<Task<IEnumerable<Lesson>>>();
		foreach (var date in dates)
		{
			tasks.Add(Task.Run(async () => await GetCalendarWeekAsync(date, ct)));
		}

		var results = await Task.WhenAll(tasks);

		return results
			.Where(t => t is not null)
			.SelectMany(t => t!)
			.DistinctBy(d => d.Date)
			.ToList();
	}

	private static List<Lesson>? ParseCalendarDays(JsonDocument jsonDoc)
	{
		List<Lesson> result = [ ];
		var rootElement = jsonDoc.RootElement;

		foreach (var dateProp in rootElement.EnumerateObject()) // date
		{
			if (!dateProp.Name.RegisterTryParse(out var DateTimeOffset))
			{
				continue;
			}

			List<Lesson> rawLessons = [ ];

			foreach (var hour in dateProp.Value.EnumerateObject())
			{
				try
				{
					var parsedLesson = hour.Value.Deserialize<Lesson>(Constants.SerializerOptions)!;

					if (parsedLesson is not null)
					{
						rawLessons.Add(parsedLesson);
					}
				}
				catch
				{
					continue;
				}
			}

			List<Lesson> compactedLessons = [ ];
			Models.DigitalesRegister.Lesson? currentLesson = null;
			foreach (var lesson in rawLessons.OrderBy(l => l.FromHour).ThenBy(l => l.ToHour).ToList())
			{
				if (currentLesson is null || !lesson.LinkToPreviousHour)
				{
					currentLesson = lesson;
					compactedLessons.Add(currentLesson);
				}
				else
				{
					currentLesson = new()
					{
						FromHour = currentLesson.FromHour,
						ToHour = lesson.ToHour,
						LinkToPreviousHour = currentLesson.LinkToPreviousHour,

						Date = currentLesson.Date,
						Id = currentLesson.Id,
						LessonId = currentLesson.LessonId,
						LessonName = currentLesson.LessonName,
						Subject = currentLesson.Subject,
						Teachers = currentLesson.Teachers,
					};
					compactedLessons[ ^1 ] = currentLesson;
				}
			}

			result.AddRange(compactedLessons);
		}

		return result;
	}

	public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
	{
		await _authenticationSemaphore.WaitAsync(ct);

		try
		{
			if (TokenExpiration is null) // not authenticated yet
			{
				var authResponse = await PostJsonAsync<TokenCreateResponse>(
					RegisterPathAPI.TokenCreate,
					new TokenCreateRequest
					{
						Code = _authCode
					},
					true,
					ct
				);

				if (authResponse is null)
				{
					return false;
				}
				else
				{
					_accessToken = authResponse.Token;
					_refreshToken = authResponse.RefreshToken;
					TokenExpiration = authResponse.ExpirationDate;
					UserId = authResponse.UserId;
					return true;
				}
			}
			else if (TokenExpiration >= DateTime.Now.AddMinutes(5)) // not expired
			{
				return true;
			}
			else
			{
				return await RefreshTokenAsync(ct); // expired, refresh

			}
		}
		finally
		{
			_authenticationSemaphore.Release();
		}
	}

	private async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
	{
		if (_refreshToken is null || UserId is null) { return false; }
		var request = new TokenRefreshRequest { RefreshToken = _refreshToken, UserId = (int)UserId! };
		var response = await PostJsonAsync<TokenRefreshResponse>(RegisterPathAPI.TokenRefresh, request, true, ct: ct);
		if (response is null) { return false; }
		_accessToken = response.Token;
		TokenExpiration = response.Expiration;
		return true;
	}

	/// <summary>
	/// Posts the json and automatically validates the object
	/// </summary>
	private async Task<T?> PostJsonAsync<T>(RegisterPath path, object? data, bool authRequest = false, CancellationToken ct = default)
	{
		try
		{
			var response = await PostJsonAsync(path, data, authRequest, ct);

			if (response is null)
			{
				return default;
			}
			else
			{
				var content = await response.ReadContentAsStringAsync(ct);
				var deserialized = JsonSerializer.Deserialize<T>(content, Constants.SerializerOptions);
				return deserialized is not null && deserialized.TryValidate() ? deserialized : default;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return default;
		}
	}

	private async Task<HttpResponseMessage?> PostJsonAsync(RegisterPath path, object? data, bool authRequest = false, CancellationToken ct = default)
	{
		try
		{
			var request = new HttpRequestMessage(HttpMethod.Post, path.Get(SchoolUri));

			if (authRequest)
			{
				ConfigureHeadersAuth(ref request);
			}
			else
			{
				if (!await AuthenticateAsync(ct))
				{
					return null;
				}
				ConfigureHeadersDefault(ref request);
			}
			request.Content = new StringContent(
				JsonSerializer.Serialize(data, Constants.SerializerOptions),
				Encoding.UTF8,
				"application/json"
			);

			var response = await SendAsync(request, ct);
			return response;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Gets and automatically validates the object
	/// </summary>
	private async Task<T?> GetAsync<T>(RegisterPath path, CancellationToken ct = default)
	{
		var response = await GetAsync(path, ct: ct);

		if (response is null || !response.TryValidate())
		{
			return default;
		}
		else
		{
			try
			{
				return JsonSerializer.Deserialize<T>(await response.ReadContentAsStringAsync(ct), Constants.SerializerOptions);
			}
			catch
			{
				return default;
			}
		}
	}

	private async Task<HttpResponseMessage?> GetAsync(RegisterPath path, Dictionary<string, string>? uriArgs = null, CancellationToken ct = default)
	{
		if (!await AuthenticateAsync(ct))
		{
			return null;
		}

		var uri = path.Get(SchoolUri);
		if (uriArgs is not null)
		{
			uri = new(QueryHelpers.AddQueryString(uri.AbsoluteUri, uriArgs));
		}

		var request = new HttpRequestMessage(HttpMethod.Get, uri);

		try
		{
			ConfigureHeadersDefault(ref request);
			return await SendAsync(request, ct);
		}
		catch
		{
			return null;
		}
	}

	private async Task<HttpResponseMessage?> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
	{
		await _requestThrottler.WaitAsync(ct);
		Console.WriteLine($"Sending request to: {request.RequestUri?.LocalPath}\t{DateTime.UtcNow}");
		try
		{
			return await _httpClient.SendAsync(request, ct);
		}
		catch
		{
			return null;
		}
	}

	private void ConfigureHeadersAuth(ref HttpRequestMessage request)
	{
		request.Headers.Add(IDigitalRegisterClient.ClientIdHeader, ClientId);
		request.Headers.Add(IDigitalRegisterClient.ApiSecretHeader, _secret);
	}

	private void ConfigureHeadersDefault(ref HttpRequestMessage request)
	{
		request.Headers.Add(IDigitalRegisterClient.ClientIdHeader, ClientId);
		request.Headers.Add(IDigitalRegisterClient.TokenHeader, _accessToken);
	}

	private bool _disposed = false;
	~DigitalRegisterClient() => Dispose(false);
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	protected void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			_clientHandler.Dispose();
			_httpClient.Dispose();
			_requestThrottler.Dispose();
		}
		_disposed = true;
	}
}
