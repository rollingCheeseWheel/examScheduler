using examScheduler.Digitales_Register_API.Models;
using examScheduler.Models.Auth;
using System;
using System.Data;
using System.Net;
using System.Text.Json;

namespace examScheduler.Digitales_Register_API;

public class RegisterClient : IDisposable
{
	public static ILogger logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug)).CreateLogger<RegisterClient>();

	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private CookieContainer _cookieContainer = new();
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	private string _registerUsername;
	private string _registerPassword;
	private bool _expired => _cookieExpiration < DateTime.UtcNow;
	private DateTime _cookieExpiration = DateTime.MinValue;

	public RegisterClient(string registerUsername, string registerPassword, Uri registerURI)
	{
		RegisterBaseURI = registerURI.GetBaseApiPath();

		_registerUsername = registerUsername;
		_registerPassword = registerPassword;

		_httpClientHandler = new HttpClientHandler()
		{
			UseCookies = true,
			CookieContainer = _cookieContainer,
		};

		_httpClient = new HttpClient(_httpClientHandler);
	}

	public RegisterClient(string registerUsername, string registerPassword, string registerURI)
		: this(registerUsername, registerPassword, new Uri(registerURI)) { }

	public RegisterClient(RegisterRequest loginRequest) : this(loginRequest.Username, loginRequest.Password, loginRequest.Uri) { }

	public async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		if (!_expired) return true;

		var credentials = new
		{
			password = _registerPassword,
			username = _registerUsername
		};

		var response = await PostJsonAsync(RegisterPath.Login, credentials, true, ct);

		var cookies = _httpClientHandler.CookieContainer.GetAllCookies();

		if (!response.IsSuccessStatusCode ||
			!cookies.Any()) return false;

		foreach (Cookie cookie in cookies)
		{
			logger.LogDebug($"Name: {cookie.Name} - Value: {cookie.Value} - Expiration: {cookie.Expires}");
		}

		_cookieExpiration = cookies
			.OrderBy((cookie) => cookie.Expires)
			.FirstOrDefault()!.Expires;

		logger.LogDebug($"Logged in: {!_expired}");
		logger.LogDebug($"Expiration date: {_cookieExpiration}");
		var message = await response.Content.ReadAsStringAsync();
		logger.LogDebug(message);
		return !_expired;
	}

	private async Task<bool> TryLoginIfNotAlreadyAsync(CancellationToken ct = default)
	{
		if (!_expired)
		{
			return true;
		}
		else
		{
			return await LoginAsync(ct);
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
		var response = await PostJsonAsync(RegisterPath.ProfileDetails, null, ct: ct);

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

		var stringContent = new StringContent(JsonSerializer.Serialize(value))
		{
			Headers = { ContentType = new("application/json") }
		};

		var uri = RegisterBaseURI.AppendRelativePath(path);

		var request = new HttpRequestMessage(HttpMethod.Post, uri)
		{
			Content = stringContent
		};

		logger.LogDebug(uri.ToString());

		return await _httpClient.SendAsync(request, ct);

		/*return await _httpClient.PostAsJsonAsync(RegisterBaseURI.AppendRelativePath(path), value, ct);*/
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