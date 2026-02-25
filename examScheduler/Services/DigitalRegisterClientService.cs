using Microsoft.AspNetCore.WebUtilities;
using Models.DigitalesRegister;
using registerClient;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Util;
using Util.Extensions;

namespace examScheduler.Services;

public interface IDigitalRegisterClientService
{
	Task<ILightWeightDigitalRegisterClient?> TryCreateClientAsync(string schoolId, string authCode, CancellationToken ct = default);
	IDigitalRegisterClient? TryGetClient(Guid clientId);

	bool TryAddSchool(string schoolId, Uri digitalRegisterUri, string clientId, string secret);
	bool HasSchool(string schoolId);
}

public class DigitalRegisterClientService(IHttpClientFactory httpClientFactory) : IDigitalRegisterClientService
{
	private readonly IHttpClientFactory _clientFactory = httpClientFactory;
	private readonly ConcurrentDictionary<string, DigitalRegisterSchool> _schools = new();
	private readonly ConcurrentDictionary<Guid, LightWeightRegisterClient> _sessions = new();

	public async Task<ILightWeightDigitalRegisterClient?> TryCreateClientAsync(string schoolId, string authCode, CancellationToken ct = default)
	{
		RemoveExpiredClients();

		if (!_schools.TryGetValue(schoolId, out var school))
		{
			return null;
		}

		var httpClient = _clientFactory.CreateClient("secure");
		httpClient.BaseAddress = school.RegisterURL;
		var client = new LightWeightRegisterClient(
			httpClient,
			new(authCode, AuthStatus.None, DateTimeOffset.MinValue),
			school
		);
		return !await client.AuthenticateAsync(ct)
			? null
			: !_sessions.TryAdd(client.Id, client) ? null : (ILightWeightDigitalRegisterClient)client;
	}

	public IDigitalRegisterClient? TryGetClient(Guid clientId)
	{
		RemoveExpiredClients();
		return _sessions.GetValueOrDefault(clientId);
	}
	public bool TryAddSchool(string schoolId, Uri digitalRegisterUri, string clientId, string secret) => _schools.TryAdd(schoolId, new(digitalRegisterUri, clientId, secret));

	public bool HasSchool(string schoolId) => _schools.ContainsKey(schoolId);

	private void RemoveExpiredClients()
	{
		var expiredClients = _sessions
			.Where(kvp => kvp.Value.IsExpired)
			.ToList();
		foreach (var expiredClient in expiredClients)
		{
			expiredClient.Value.Dispose();
			_sessions.Remove(expiredClient.Key, out var _);
		}
	}
}

public interface ILightWeightDigitalRegisterClient : IDigitalRegisterClient
{
	Guid Id { get; }
}

/// <summary>
/// Do not dispose of the client, it's lightweight and can be reobtained through its id
/// </summary>
public class LightWeightRegisterClient : ILightWeightDigitalRegisterClient, IDisposable
{
	public Guid Id { get; } = Guid.CreateVersion7();
	private readonly HttpClient _httpClient;
	private ClientSession _session;
	private readonly DigitalRegisterSchool _school;
	private readonly Lock _lock = new();
	private readonly SemaphoreSlim _authSemaphore = new(1);

	internal LightWeightRegisterClient(HttpClient configuredHttpClient, ClientSession session, DigitalRegisterSchool school)
	{
		_httpClient = configuredHttpClient;
		_session = session;
		_school = school;
	}

	public AuthStatus AuthStatus => _session.AuthStatus;
	public DateTimeOffset SessionExpiration => _session.SessionExpiration;
	public bool IsExpired => SessionExpiration <= DateTimeOffset.UtcNow.AddMinutes(1) || AuthStatus is AuthStatus.Failed;

	public RegisterUserProfile? UserProfile { get; private set; }
	public long? UserId { get; private set; }

	public async Task<UserRoles?> GetRoleAsync(CancellationToken ct = default)
	{
		UserProfile ??= await GetUserProfileAsync(ct);
		return IDigitalRegisterClient.GetRole(UserProfile);
	}

	public async Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct = default)
	{
		var tempProfile = await GetAsync<RegisterUserProfile>(RegisterPathAPI.UserProfile, ct: ct);
		using (_lock.EnterScope())
		{
			UserProfile = tempProfile;
		}
		return UserProfile;
	}

	public async Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct = default) => await GetAsync<ICollection<RegisterClass>>(RegisterPathAPI.Classes, ct: ct);

	public async Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct = default) => await GetAsync<ICollection<RegisterSubject>>(RegisterPathAPI.Subjects, ct);

	public async Task<IEnumerable<Lesson>> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct = default)
	{
		var args = new Dictionary<string, string?> { { "startDate", date.RoundDownToMonday().ToRegisterFormat() } };

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

	public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
	{
		await _authSemaphore.WaitAsync(ct);

		_lock.Enter();
		var status = AuthStatus;
		var expiration = SessionExpiration;
		var authCode = _session.AuthCode;
		_lock.Exit();
		try
		{
			if (status is AuthStatus.Failed)
			{
				return false;
			}
			else if (status is AuthStatus.None)
			{
				var authResponse = await PostJsonAsync<TokenCreateResponse>(
					RegisterPathAPI.TokenCreate,
					new TokenCreateRequest
					{
						Code = authCode
					},
					true,
					ct
				);

				if (authResponse is null)
				{
					using (_lock.EnterScope())
					{
						_session = _session with
						{
							AuthStatus = AuthStatus.Failed,
						};
					}
					return false;
				}
				else
				{
					using (_lock.EnterScope())
					{
						_session = _session with
						{
							AccessToken = authResponse.Token,
							RefreshToken = authResponse.RefreshToken,
							SessionExpiration = authResponse.ExpirationDate,
							AuthStatus = AuthStatus.Authenticated
						};
						UserId = authResponse.UserId;
					}
					return true;
				}
			}
			else if (status is AuthStatus.Authenticated && expiration <= DateTimeOffset.UtcNow.AddMinutes(1))
			{
				return await RefreshTokenAsync(ct);
			}
			return expiration >= DateTimeOffset.UtcNow.AddMinutes(1);
		}
		finally
		{
			_authSemaphore.Release();
		}
	}

	private async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
	{
		_lock.Enter();
		if (_session.RefreshToken is null || UserId is null)
		{
			return false;
		}
		var request = new TokenRefreshRequest()
		{
			RefreshToken = _session.RefreshToken,
			UserId = (int)UserId!
		};
		_lock.Exit();

		var response = await PostJsonAsync<TokenRefreshResponse>(RegisterPathAPI.TokenRefresh, request, true, ct: ct);
		if (response is null)
		{
			return false;
		}
		using (_lock.EnterScope())
		{
			_session = _session with
			{
				RefreshToken = null,
				AuthStatus = AuthStatus.Refreshed,
				AccessToken = response.Token,
				SessionExpiration = response.Expiration,
			};
		}
		return true;
	}

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
			_lock.Enter();
			var request = new HttpRequestMessage(HttpMethod.Post, path.Get(_school.RegisterURL));
			_lock.Exit();

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

	private async Task<HttpResponseMessage?> GetAsync(RegisterPath path, Dictionary<string, string?>? uriArgs = null, CancellationToken ct = default)
	{
		if (!await AuthenticateAsync(ct))
		{
			return null;
		}

		_lock.Enter();
		var uri = path.Get(_school.RegisterURL);
		_lock.Exit();
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
		using (_lock.EnterScope())
		{
			request.Headers.Add(IDigitalRegisterClient.ClientIdHeader, _school.ClientId);
			request.Headers.Add(IDigitalRegisterClient.ApiSecretHeader, _school.Secret);
		}
	}

	private void ConfigureHeadersDefault(ref HttpRequestMessage request)
	{
		using (_lock.EnterScope())
		{
			request.Headers.Add(IDigitalRegisterClient.ClientIdHeader, _school.ClientId);
			request.Headers.Add(IDigitalRegisterClient.TokenHeader, _session.AccessToken);
		}
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
			Lesson? currentLesson = null;
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

	#region Disposable
	private bool _disposed = false;
	~LightWeightRegisterClient() => Dispose(true);
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
			_httpClient.Dispose();
		}
		_disposed = true;
	}
	#endregion
}

internal sealed record DigitalRegisterSchool(Uri RegisterURL, string ClientId, string Secret);

internal sealed record ClientSession(string AuthCode, AuthStatus AuthStatus, DateTimeOffset SessionExpiration, string? AccessToken = null, string? RefreshToken = null);

public enum AuthStatus
{
	None,
	Authenticated,
	Refreshed,
	Failed
}