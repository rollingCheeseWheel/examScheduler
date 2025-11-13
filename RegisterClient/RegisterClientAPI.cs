using Entities;
using Microsoft.AspNetCore.DataProtection;
using Models.DigitalesRegister;
using Models.DigitalesRegister.old;
using System.Net.Http.Json;
using System.Text.Json;
using Util;

namespace registerClient;

public class RegisterClientAPI(
	Uri schoolUri,
	int schoolId,
	string code
) : IDisposable, IRegisterClient
{
	public readonly Uri SchoolUri = schoolUri.GetSchemeAndAuthority();
	public readonly int SchoolId = schoolId;
	private readonly HttpClient _httpClient = new HttpClient();

	private string _authCode = code;
	private string? _accessToken;
	private string? _refreshToken;
	private DateTimeOffset? _tokenExpiration;
	private int? _userId;

	public async Task<bool> AuthenticateAsync(string apiSecret, CancellationToken ct = default)
	{
		var authResponse = await PostJsonAsync<TokenCreateResponse>(RegisterPathAPI.TokenCreate, new TokenCreateRequest { Code = _authCode }, apiSecret, ct);

		if (authResponse is null)
		{
			return false;
		}
		else if (authResponse.TryValidate())
		{
			return false;
		}
		else
		{
			_accessToken = authResponse.Token;
			_refreshToken = authResponse.RefreshToken;
			_tokenExpiration = authResponse.ExpirationDate;
			_userId = authResponse.UserId;
			return true;
		}
	}

	public Task<Models.DigitalesRegister.CalendarWeek?> GetCalendarWeekAsync(DateTimeOffset date, CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public Task<Models.DigitalesRegister.Calendar?> GetCompleteCalendarAsync(CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public Task<RegisterProfileModel?> GetUserProfileAsync(CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public UserProfileRoles GetUserRole(RegisterProfileModel profile)
	{
		throw new NotImplementedException();
	}

	public Task<bool> ValidateCredentials(CancellationToken ct)
	{
		throw new NotImplementedException();
	}


	private async Task<T?> PostJsonAsync<T>(RegisterPath path, object? data, string? apiSecret = null, CancellationToken ct = default)
	{
		var response = await PostJsonAsync(path, data, apiSecret, ct);
		var responseString = await response.Content.ReadAsStringAsync();

		if (responseString is null)
		{
			return default;
		}
		else
		{
			try
			{
				return JsonSerializer.Deserialize<T>(responseString, Constants.SerializerOptions);
			}
			catch
			{
				return default;
			}
		}
	}

	private async Task<HttpResponseMessage> PostJsonAsync(RegisterPath path, object? data, string? apiSecret = null, CancellationToken ct = default)
	{
		if (apiSecret is not null)
		{
			ConfigureHeadersAuth(apiSecret);
		}
		else
		{
			ConfigureHeardsDefault();
		}

		return await _httpClient.PostAsJsonAsync(path.Get(SchoolUri), data, Constants.SerializerOptions, ct);
	}

	private void ConfigureHeadersAuth(string secret)
	{
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add(ClientIdHeader, SchoolId.ToString());
		_httpClient.DefaultRequestHeaders.Add(ApiSecretHeader, secret);
	}

	private void ConfigureHeardsDefault()
	{
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add(ClientIdHeader, SchoolId.ToString());
		_httpClient.DefaultRequestHeaders.Add(TokenHeader, _accessToken);
	}

	public const string ClientIdHeader = "API-CLIENT-ID";
	public const string ApiSecretHeader = "API-SECRET";
	public const string TokenHeader = "API-TOKEN";

	private bool _disposed = false;
	~RegisterClientAPI() => Dispose(false);
	public void Dispose() => Dispose(true);
	protected void Dispose(bool finalizing)
	{
		if (_disposed)
		{
			return;
		}
		if (finalizing)
		{
			GC.SuppressFinalize(this);
		}
	}
}
