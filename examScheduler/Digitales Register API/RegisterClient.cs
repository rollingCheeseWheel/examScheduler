using examScheduler.Digitales_Register_API.Models;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace examScheduler.Digitales_Register_API;

public class RegisterClient : IDisposable
{
	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	private string? _registerUsername;
	private string? _registerPassword;

	private bool _loggedIn = false;
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

	private async Task<bool> LoginAsync(CancellationToken ct = default)
	{
		if (_registerPassword is null || _registerUsername is null) return true;

		var credentials = new LoginRequest
		{
			Password = _registerPassword,
			Username = _registerUsername
		};

		var (parsedResponse, response, error) = await PostJsonAsync<LoginResponse>(RegisterPath.Login, credentials, true, ct);

		if (!_httpClientHandler.CookieContainer.GetAllCookies().Any()
			|| parsedResponse is null)
			return false;

		_cookieExpiration = _httpClientHandler.CookieContainer.GetAllCookies()
			.OrderBy((cookie) => cookie.Expires)
			.FirstOrDefault()!.Expires;

		_loggedIn = parsedResponse.LoggedIn;

		return true;
	}

	private async Task<bool> TryLoginIfNotAlreadyAsync(CancellationToken ct = default)
	{
		if (_loggedIn && DateTime.UtcNow < _cookieExpiration)
		{
			return true;
		}
		else if (_loggedIn)
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

		while (stopDate <= iterDate)
		{

		}

		return new();
	}

	private async Task<HttpResponseMessage> PostJsonAsync(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default)
	{
		if (!isAuthRequest && await TryLoginIfNotAlreadyAsync(ct))
			throw new InvalidOperationException("The user cannot be logged in");
		return await _httpClient.PostAsJsonAsync(path, value, Constants.SerializerOptions, ct);
	}

	private async Task<(T?, HttpResponseMessage?, Exception?)> PostJsonAsync<T>(RegisterPath path, object? value, bool isAuthRequest = false, CancellationToken ct = default) where T : class
	{
		try
		{
			var response = await PostJsonAsync(path, value, isAuthRequest, ct);
			var message = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var deserialized = JsonSerializer.Deserialize<T>(message, Constants.SerializerOptions);
				return (deserialized, response, null);
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

internal class ResponseWrapper<T> where T : class
{
	public HttpResponseMessage ResponseMessage { get; init; }
	public T? Value { get; init; } = null;

	private ResponseWrapper(HttpResponseMessage responseMessage, T? value = null)
	{
		ResponseMessage = responseMessage;
		Value = value;
	}

	public async Task<ResponseWrapper<T>> Create(HttpResponseMessage responseMessage)
	{
		T? value = null;

		try
		{
			var message = await responseMessage.Content.ReadAsStringAsync();

			value = JsonSerializer.Deserialize<T>(message, Constants.SerializerOptions);
		}
		catch
		{
			value = null;
		}

		return new ResponseWrapper<T>(responseMessage, responseMessage.IsSuccessStatusCode
			? value
			: null
		);
	}
}