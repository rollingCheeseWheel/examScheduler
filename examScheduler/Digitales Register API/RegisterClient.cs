using examScheduler.Digitales_Register_API.Models;
using examScheduler.Models.Auth;
using System.Data;
using System.Text.Json;

namespace examScheduler.Digitales_Register_API;

public class RegisterClient : IDisposable
{
	public static ILogger logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug)).CreateLogger<RegisterClient>();

	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	private string _registerUsername;
	private string _registerPassword;

	private bool _loggedIn = false;
	private bool _expired => _cookieExpiration > DateTime.UtcNow;
	private DateTime _cookieExpiration = DateTime.MaxValue;

	public RegisterClient(string registerUsername, string registerPassword, Uri registerURI)
	{
		RegisterBaseURI = registerURI.GetBaseApiPath();

		_registerUsername = registerUsername;
		_registerPassword = registerPassword;

		_httpClientHandler = new HttpClientHandler()
		{
			UseCookies = true,
			CookieContainer = new(),
		};

		_httpClient = new HttpClient(_httpClientHandler)
		{
			BaseAddress = RegisterBaseURI
		};
	}

	public RegisterClient(string registerUsername, string registerPassword, string registerURI)
		: this(registerUsername, registerPassword, new Uri(registerURI)) { }

	public RegisterClient(RegisterRequest loginRequest) : this(loginRequest.Username, loginRequest.Password, loginRequest.Uri) { }

	private async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		if (_loggedIn && !_expired) return _loggedIn;

		var credentials = new Models.LoginRequest
		{
			Password = _registerPassword,
			Username = _registerUsername
		};

		var (parsedResponse, response, error) = await PostJsonAsync<Models.LoginResponse>(RegisterPath.Login, credentials, true, ct);

		if (!_httpClientHandler.CookieContainer.GetAllCookies().Any()
			|| parsedResponse is null)
		{
			logger.LogDebug("Exiting early");
			logger.LogDebug(JsonSerializer.Serialize(parsedResponse));
			logger.LogDebug(error?.Message);
			logger.LogDebug(response.Headers.ToString());
			return false;
		}

		var cookies = _httpClientHandler.CookieContainer.GetAllCookies().AsEnumerable() ?? [ ];

		foreach (var cookie in cookies)
		{
			logger.LogDebug($"Name: {cookie.Name} - Value: {cookie.Value}");
		}

		_cookieExpiration = cookies
			.OrderBy((cookie) => cookie.Expires)
			.FirstOrDefault()!.Expires;

		_loggedIn = parsedResponse.LoggedIn && !_expired;

		logger.LogDebug($"Logged in: {_loggedIn}");

		return _loggedIn;
	}

	private async Task<bool> TryLoginIfNotAlreadyAsync(CancellationToken ct = default)
	{
		if (_loggedIn && !_expired)
		{
			return true;
		}
		else if (_expired)
		{
			return await LoginAsync(ct);
		}
		else
		{
			return false;
		}
	}

	public async Task<List<CalendarDay>> GetCalendarAsync(DateTime startDate, int spanYears = 1, CancellationToken ct = default)
	{
		var iterDate = startDate;
		var stopDate = startDate.AddYears(spanYears);

		List<CalendarDay> days = new();
		HttpResponseMessage response;

		while (stopDate >= iterDate)
		{
			response = await PostJsonAsync(RegisterPath.Calendar, new CalendarRequest(iterDate), ct: ct);

			days.AddRange(await ParseCalendarDays(response));

			iterDate = iterDate.AddDays(7);
		}

		return new();
	}

	public async Task<string> GetProfileDetailsAsync(CancellationToken ct = default)
	{
		var response = await PostJsonAsync(RegisterPath.ProfileDetails, new { }, ct: ct);

		return await response.Content.ReadAsStringAsync(ct);
	}

	private static async Task<List<CalendarDay>> ParseCalendarDays(HttpResponseMessage response, CancellationToken ct = default)
	{
		List<CalendarDay> calendarDays = new();

		var message = await response.Content.ReadAsStringAsync(ct);
		var jsonDoc = JsonDocument.Parse(message);
		var root = jsonDoc.RootElement;

		foreach (var prop in root.EnumerateObject()) // date
		{
			if (!prop.Name.RegisterTryParse(out var dateTime))
				continue;

			List<HourInDay> hoursInDay = new();

			try
			{
				var nestedProp = prop.Value.EnumerateObject().First(); // "1"
				var innerNestedProp = nestedProp.Value.EnumerateObject().First(); // "1"

				foreach (var hour in innerNestedProp.Value.EnumerateObject())
				{
					try
					{
						hoursInDay.Append(JsonSerializer.Deserialize<HourInDay>(hour.Value, Constants.SerializerOptions));
					}
					catch
					{
						continue;
					}
				}
			}
			catch
			{
				continue;
			}
		}

		return calendarDays;
	}

	private async Task<HttpResponseMessage> PostJsonAsync(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default)
	{
		if (!isAuthRequest && await TryLoginIfNotAlreadyAsync(ct))
			throw new InvalidOperationException("The user cannot be logged in");

		logger.LogDebug(path);
		logger.LogDebug(_httpClient.BaseAddress.ToString());
		return await _httpClient.PostAsJsonAsync(path, value, Constants.SerializerOptions, ct);
	}

	private async Task<(T?, HttpResponseMessage?, Exception?)> PostJsonAsync<T>(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default) where T : class
	{
		try
		{
			var response = await PostJsonAsync(path, value, isAuthRequest, ct);
			var message = await response.Content.ReadAsStringAsync(ct);

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

	~RegisterClient()
	{
		Dispose();
	}

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