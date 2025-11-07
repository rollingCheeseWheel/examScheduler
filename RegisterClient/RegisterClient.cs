using System.Net;
using System.Text.Json;
using Models.API;
using Models.DigitalesRegister;
using Util;

namespace registerClient;

// when extended to multiple Digitales Register versions could be used to implement multiple adapters
public interface IRegisterClient
{
	/// <summary>
	/// Returns the Role of the user, Unknown by default
	/// </summary>
	/// <param name="profile"></param>
	/// <returns></returns>
	public static Entities.UserProfileRoles GetUserRole(RegisterProfileModel profile)
	{
		string[ ] student = [ "schüler/in", "alunno", "student" ];
		string[ ] teacher = [ ];

		return profile.RoleName.ToLower() switch
		{
			var f when student.Contains(f) => Entities.UserProfileRoles.Student,
			var f when teacher.Contains(f) => Entities.UserProfileRoles.Teacher,
			_ => Entities.UserProfileRoles.Unknown,
		};
	}

	Task<bool> ValidateCredentials(CancellationToken ct);
	Task<RegisterProfileModel?> GetUserProfileAsync(CancellationToken ct);
	Task<Calendar?> GetCompleteCalendarAsync(CancellationToken ct);
	Task<CalendarWeek?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct);
}

public class RegisterClient : IDisposable, IRegisterClient
{
	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private CookieContainer _cookieContainer = new();
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	private readonly string _registerUsername;
	private readonly string _registerPassword;

	public bool LoggedIn { get; private set; }

	public RegisterClient(string registerUsername, string registerPassword, Uri registerURI)
	{
		if (registerURI.Scheme != Uri.UriSchemeHttps)
		{
			throw new ArgumentException(nameof(registerURI), "Invalid URI scheme, must be HTTPS");
		}

		RegisterBaseURI = registerURI.GetBaseApiPath();

		_registerUsername = registerUsername;
		_registerPassword = registerPassword;

		_httpClientHandler = new HttpClientHandler()
		{
			UseCookies = true,
			CookieContainer = _cookieContainer,
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
			{
				if (errors != System.Net.Security.SslPolicyErrors.None)
					return false;

				if (message.RequestUri?.Scheme != Uri.UriSchemeHttps)
					return false;

				return true;
			}
		};

		_httpClient = new HttpClient(_httpClientHandler)
		{
			BaseAddress = RegisterBaseURI,
		};
	}

	public RegisterClient(string registerUsername, string registerPassword, string registerURI)
		: this(registerUsername, registerPassword, new Uri(registerURI)) { }

	public RegisterClient(SignupRequest loginRequest)
		: this(loginRequest.Username, loginRequest.Password, loginRequest.RegisterUri) { }

	public async Task<bool> ValidateCredentials(CancellationToken ct) => await TryLoginIfNotAlreadyAsync(ct);

	public async Task<RegisterProfileModel?> GetUserProfileAsync(CancellationToken ct = default)
	{
		try
		{
			return JsonSerializer.Deserialize<RegisterProfileModel>(await GetUserProfileStringAsync(ct), Constants.SerializerOptions);
		}
		catch
		{
			return null;
		}
	}


	public async Task<Calendar?> GetCompleteCalendarAsync(CancellationToken ct = default)
	{
		var calendarWeeks = new List<CalendarWeek>();
		var iterDate = DateTimeOffset.UtcNow;
		while (iterDate < DateTimeOffset.UtcNow.AddYears(1))
		{
			//Console.WriteLine($"Getting calendar for week {iterDate}");
			var tempWeek = await GetCalendarWeekAsync(iterDate, ct);
			iterDate = iterDate.AddDays(7);

			if (tempWeek is not null)
			{
				calendarWeeks.Add(tempWeek);
			}
		}

		return calendarWeeks.Count != 0 ? new(calendarWeeks) : null;
	}

	public async Task<CalendarWeek?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct = default)
	{
		var json = await GetCalendarWeekStringAsync(date, ct);
		return json is null ? null : ParseCalendarWeek(json);
	}

	private async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		if (LoggedIn) return true;

		var credentials = new LoginRequest
		{
			Password = _registerPassword,
			Username = _registerUsername
		};

		var (loginResponse, response, error) = await PostJsonAsync<LoginResponse>(RegisterPath.Login, credentials, true, ct);

		var cookies = _httpClientHandler.CookieContainer.GetAllCookies();

		if (error is not null
			|| loginResponse is null
			|| !response!.IsSuccessStatusCode
			|| !cookies.Any())
		{
			return false;
		}

		LoggedIn = loginResponse.LoggedIn.GetValueOrDefault();

		return LoggedIn;
	}

	private async Task<bool> TryLoginIfNotAlreadyAsync(CancellationToken ct = default)
	{
		return LoggedIn ? true : await LoginAsync(ct);
	}

	private async Task<string> GetUserProfileStringAsync(CancellationToken ct = default)
	{
		var (response, error) = await PostJsonAsync(RegisterPath.ProfileDetails, new { }, ct: ct);

		if (error is not null) return error.Message;

		return await response!.Content.ReadAsStringAsync(ct);
	}

	private async Task<string?> GetCalendarWeekStringAsync(DateTimeOffset date, CancellationToken ct = default)
	{
		var (response, error) = await PostJsonAsync(RegisterPath.Calendar, new CalendarRequest { StartDate = date.RoundToMonday() }, ct: ct);

		if (response is null)
			return null;

		return await response.ReadContentAsStringAsync(ct);
	}

	private static CalendarWeek ParseCalendarWeek(string json)
	{
		List<CalendarDay> calendarDays = new();

		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		foreach (var prop in root.EnumerateObject()) // date
		{
			if (!prop.Name.RegisterTryParse(out var DateTimeOffset))
			{
				continue;
			}

			List<Models.DigitalesRegister.HourInDay> hoursInDay = new();

			List<JsonProperty> flattenedEnumeratedObject = [ ];

			foreach (var nestedProp in prop.Value.EnumerateObject())
			{
				foreach (var innerMostProp in nestedProp.Value.EnumerateObject())
				{
					flattenedEnumeratedObject.AddRange(innerMostProp.Value.EnumerateObject());
				}
			}

			foreach (var hour in flattenedEnumeratedObject)
			{
				try
				{
					var parsedHour = hour.Value.Deserialize<Models.DigitalesRegister.HourInDay>(Constants.SerializerOptions)!;


					if (parsedHour.IsLesson)
					{
						hoursInDay.Add(parsedHour);
					}
					else
					{
						continue;
					}
				}
				catch
				{
					continue;
				}
			}

			calendarDays.Add(new()
			{
				Date = (DateTimeOffset)DateTimeOffset!,
				HoursInDay = hoursInDay
			});
		}

		return new() { Days = calendarDays };
	}

	private async Task<(HttpResponseMessage?, Exception?)> PostJsonAsync(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default)
	{
		try
		{
			if (!isAuthRequest && !await TryLoginIfNotAlreadyAsync(ct))
				throw new InvalidOperationException("The user cannot be logged in");


			var stringContent = new StringContent(JsonSerializer.Serialize(value, Constants.SerializerOptions))
			{
				Headers = { ContentType = new("application/json") }
			};

			var request = new HttpRequestMessage(HttpMethod.Post, RegisterBaseURI.AppendRelativePath(path))
			{
				Content = stringContent
			};

			return (await _httpClient.SendAsync(request, ct), null);
		}
		catch (Exception ex)
		{
			return (null, ex);
		}
	}

	private async Task<(T?, HttpResponseMessage?, Exception?)> PostJsonAsync<T>(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default) where T : class
	{
		try
		{
			var (response, error) = await PostJsonAsync(path, value, isAuthRequest, ct);

			if (error is not null) return (null, response, error);

			var message = await response!.Content.ReadAsStringAsync(ct);

			if (response.IsSuccessStatusCode)
			{
				try
				{
					var deserialized = JsonSerializer.Deserialize<T>(message, Constants.SerializerOptions);
					return (deserialized, response, null);
				}
				catch (Exception ex)
				{
					return (null, response, ex);
				}
			}

			return (null, response, null);
		}
		catch (Exception ex)
		{
			return (null, null, ex);
		}
	}

	~RegisterClient() => Dispose();
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			// Free managed resources
			_httpClient.Dispose();
			_httpClientHandler.Dispose();
		}

		// Free unmanaged resources here if you had any

		_disposed = true;
	}
}