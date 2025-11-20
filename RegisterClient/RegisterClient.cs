using Entities;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.WebUtilities;
using Models.DigitalesRegister;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Util;

namespace registerClient;

public partial class RegisterClient : IDisposable, IRegisterClient
{
	public readonly Uri SchoolUri;
	public readonly string SchoolId;
	private readonly HttpClient _httpClient;
	private readonly HttpClientHandler _clientHandler;

	private readonly string _authCode;
	private readonly string _secret;
	private string? _accessToken;
	private string? _refreshToken;
	public DateTimeOffset? TokenExpiration { get; private set; }
	public int? UserId { get; private set; }

	public RegisterUserProfile? UserProfile { get; private set; }

	public RegisterClient(Entities.School school, string authCode) : this(school.RegisterUri, school.SchoolId, school.Secret, authCode) { }

	public RegisterClient(Uri schoolUri, string schoolId, string secret, string authCode)
	{
		SchoolUri = schoolUri.GetSchemeAndAuthority();
		SchoolId = schoolId;
		_authCode = authCode;
		_clientHandler = new()
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
			{
				if (errors != System.Net.Security.SslPolicyErrors.None)
					return false;

				if (message.RequestUri?.Scheme != Uri.UriSchemeHttps)
					return false;

				return true;
			}
		};
		_httpClient = new(_clientHandler);
		_secret = secret;
	}

	public async Task<UserProfileRole?> GetRoleAsync(CancellationToken ct = default)
	{
		if (UserProfile is null)
		{
			UserProfile = await GetUserProfileAsync(ct);
		}

		return UserProfile?.Role switch
		{
			"student" => UserProfileRole.Student,
			"teacher" => UserProfileRole.Teacher,
			"admin" => UserProfileRole.Admin,
			"parent" => UserProfileRole.Parent,
			_ => null
		};
	}

	public async Task<RegisterUserProfile?> GetUserProfileAsync(CancellationToken ct = default)
	{
		var response = await GetAsync<RegisterUserProfile>(RegisterPathAPI.UserProfile, ct);
		UserProfile = response;
		return response;
	}

	public async Task<IEnumerable<RegisterClass>?> GetClassesAsync(CancellationToken ct = default) => await GetAsync<IEnumerable<RegisterClass>>(RegisterPathAPI.Classes, ct);

	public async Task<IEnumerable<RegisterSubject>?> GetSubjectsAsync(CancellationToken ct = default) => await GetAsync<IEnumerable<RegisterSubject>>(RegisterPathAPI.Subjects, ct);

	public async Task<IEnumerable<Models.DigitalesRegister.CalendarDay>?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct = default)
	{
		var args = new Dictionary<string, string> { { "startDate", date.ToRegisterFormat() } };

		var response = await GetAsync(RegisterPathAPI.LessonWeek, args, ct);
		if (response is null) return null;
		return ParseCalendarDays(JsonDocument.Parse(await response.ReadContentAsStringAsync(ct)));
	}

	public async Task<IEnumerable<Models.DigitalesRegister.CalendarDay>?> GetUpcomingCalendarAsync(CancellationToken ct = default)
	{
		var response = await GetAsync(RegisterPathAPI.LessonMonth, ct: ct);
		if (response is null) return null;
		return ParseCalendarDays(JsonDocument.Parse(await response.ReadContentAsStringAsync(ct)));
	}

	private IEnumerable<Models.DigitalesRegister.CalendarDay>? ParseCalendarDays(JsonDocument jsonDoc)
	{
		Console.WriteLine(jsonDoc.ToString());
		throw new NotImplementedException();

		/*List<CalendarDay> calendarDays = [ ];
		var root = jsonDoc.RootElement;

		foreach (var prop in root.EnumerateObject()) // date
		{
			if (!prop.Name.RegisterTryParse(out var DateTimeOffset))
			{
				continue;
			}

			List<HourInDay> hoursInDay = [ ];

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
					var parsedHour = hour.Value.Deserialize<HourInDay>(Constants.SerializerOptions)!;


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

		return calendarDays;*/
	}

	private async Task<bool> AuthenticateAsync(CancellationToken ct = default)
	{
		if (TokenExpiration is null) // not authenticated yet
		{
			var authResponse = await PostJsonAsync<TokenCreateResponse>(RegisterPathAPI.TokenCreate, new TokenCreateRequest { Code = _authCode }, true, ct);

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
		else if (TokenExpiration >= DateTimeOffset.UtcNow) // not expired
		{
			return true;
		}
		else
		{
			return await RefreshTokenAsync(ct); // expired, refresh
		}
	}

	private async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
	{
		if (_refreshToken is null || UserId is null) return false;
		var request = new TokenRefreshRequest { RefreshToken = _refreshToken, UserId = (int)UserId! };
		var response = await PostJsonAsync<TokenRefreshResponse>(RegisterPathAPI.TokenRefresh, request, true, ct: ct);
		if (response is null) return false;
		_accessToken = response.Token;
		TokenExpiration = response.Expiration;
		return true;
	}

	/// <summary>
	/// Posts the json and automatically validates the object
	/// </summary>
	private async Task<T?> PostJsonAsync<T>(RegisterPath path, object? data, bool authRequest = false, CancellationToken ct = default)
	{
		var response = await PostJsonAsync(path, data, authRequest, ct);

		if (response is null || !response.TryValidate())
		{
			return default;
		}
		else
		{
			try
			{
				return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(ct), Constants.SerializerOptions);
			}
			catch
			{
				return default;
			}
		}
	}

	private async Task<HttpResponseMessage?> PostJsonAsync(RegisterPath path, object? data, bool authRequest = false, CancellationToken ct = default)
	{
		try
		{
			if (authRequest)
			{
				ConfigureHeadersAuth();
			}
			else
			{
				if (!( await AuthenticateAsync(ct) ))
				{
					return null;
				}
				ConfigureHeadersDefault();
			}
			return await _httpClient.PostAsJsonAsync(path.Get(SchoolUri), data, Constants.SerializerOptions, ct);
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
				return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(ct), Constants.SerializerOptions);
			}
			catch
			{
				return default;
			}
		}
	}

	private async Task<HttpResponseMessage?> GetAsync(RegisterPath path, Dictionary<string, string>? uriArgs = null, CancellationToken ct = default)
	{
		var uri = path.Get(SchoolUri);
		if (uriArgs is not null)
		{
			uri = new(QueryHelpers.AddQueryString(uri.AbsoluteUri, uriArgs));
		}
		try
		{
			ConfigureHeadersDefault();
			return await _httpClient.GetAsync(uri, ct);
		}
		catch
		{
			return null;
		}
	}

	private void ConfigureHeadersAuth()
	{
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add(ClientIdHeader, SchoolId);
		_httpClient.DefaultRequestHeaders.Add(ApiSecretHeader, _secret);
	}

	private void ConfigureHeadersDefault()
	{
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add(ClientIdHeader, SchoolId.ToString());
		_httpClient.DefaultRequestHeaders.Add(TokenHeader, _accessToken);
	}

	public const string ClientIdHeader = "API-CLIENT-ID";
	public const string ApiSecretHeader = "API-SECRET";
	public const string TokenHeader = "API-TOKEN";

	private bool _disposed = false;
	~RegisterClient() => Dispose(false);
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
		}
		_disposed = true;
	}

	[GeneratedRegex(@"^https://(.*?).digitalesregister.it.*$")]
	private static partial Regex SchoolIdRegex();

}
