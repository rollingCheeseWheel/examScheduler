using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Models.DigitalesRegister;
using Models.Auth;
using Util;
using Entities;

namespace registerClient;

public class RegisterClient : IDisposable
{
	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private CookieContainer _cookieContainer = new();
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	private string _registerUsername;
	private string _registerPassword;

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

	public RegisterClient(RegisterRequest loginRequest)
		: this(loginRequest.Username, loginRequest.RegisterPassword, loginRequest.RegisterUri) { }

	private async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		if (LoggedIn) return true;

		var credentials = new Models.DigitalesRegister.LoginRequest
		{
			Password = _registerPassword,
			Username = _registerUsername
		};

		var (loginResponse, response, error) = await PostJsonAsync<Models.DigitalesRegister.LoginResponse>(RegisterPath.Login, credentials, true, ct);

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

	public string GetProfileImageAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	internal async Task<string> GetUserProfileStringAsync(CancellationToken ct = default)
	{
		var (response, error) = await PostJsonAsync(RegisterPath.ProfileDetails, new { }, ct: ct);

		if (error is not null) return error.Message;

		return await response!.Content.ReadAsStringAsync(ct);
	}

	public async Task<RegisterProfile?> GetUserProfileAsync(CancellationToken ct = default)
	{
		try
		{
			return JsonSerializer.Deserialize<RegisterProfile>(await GetUserProfileStringAsync(ct), Constants.SerializerOptions);
		} catch
		{
			return null;
		}
	}

	public async Task<List<Models.DigitalesRegister.CalendarWeek>?> GetCompleteCalendar(int yearDuration = 1, int timeoutAfterEmptyWeeks = 3, CancellationToken ct = default)
	{
		var calendarWeeks = new List<Models.DigitalesRegister.CalendarWeek>();
		var iterDateTime = DateTime.UtcNow;
		var currentWeekTimeout = 0;
		while (iterDateTime <= DateTime.UtcNow.AddYears(yearDuration) 
			&& currentWeekTimeout < timeoutAfterEmptyWeeks)
		{
			var tempWeek = await GetCalendarWeekAsync(iterDateTime, ct);
			iterDateTime.AddDays(7);

			if (tempWeek is null || !tempWeek.Days.Any())
			{
				currentWeekTimeout++;
				continue;
			}
		}

		return calendarWeeks.Any() ? calendarWeeks : null;
	}

	public async Task<Models.DigitalesRegister.CalendarWeek?> GetCalendarWeekAsync(DateTime date, CancellationToken ct = default)
	{
		var json = await GetCalendarWeekStringAsync(date, ct);
		return json is null ? null : ParseCalendarWeek(json);
	}

	private async Task<string?> GetCalendarWeekStringAsync(DateTime date, CancellationToken ct = default)
	{
		var (response, error) = await PostJsonAsync(RegisterPath.Calendar, new CalendarRequest { StartDate = date.RoundToMonday() }, ct: ct);

		Console.WriteLine(error?.Message);

		if (response is null)
			return null;

		return await response.ReadContentAsStringAsync(ct);
	}

	private static Models.DigitalesRegister.CalendarWeek ParseCalendarWeek(string json)
	{
		List<Models.DigitalesRegister.CalendarDay> calendarDays = new();

		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		foreach (var prop in root.EnumerateObject()) // date
		{
			if (!prop.Name.RegisterTryParse(out var dateTime))
			{
				continue;
			}

			List<Models.DigitalesRegister.HourInDay> hoursInDay = new();

			List<JsonProperty> flattenedEnumeratedObject = new();

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


					if (parsedHour.IsLesson){
						hoursInDay.Add(parsedHour);
					} else
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
				Date = (DateTime)dateTime!,
				HoursInDay = hoursInDay
			});
		}

		return new()
		{
			Days = calendarDays,
		};
	}

	/*private static async Task<List<CalendarDay>> ParseCalendarDays(HttpResponseMessage response, CancellationToken ct = default)
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
	}*/

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