using examScheduler.Digitales_Register_API.Models;
using System;
using System.Net;
using System.Text.Json;

namespace examScheduler.Digitales_Register_API;

public class RegisterClient : IDisposable
{
	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private HttpClient _httpClient;

	public readonly Uri RegisterBaseURI;

	public readonly Uri LoginUri;
	public readonly Uri CalendarUri;

	private string? _registerUsername;
	private string? _registerPassword;

	private bool _loggedIn = false;
	private DateTime _cookieExpiration = DateTime.MaxValue;

	public RegisterClient(string registerUsername, string registerPassword, Uri registerURI)
	{
		RegisterBaseURI = new(registerURI.Authority);
		LoginUri = RegisterBaseURI.;



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

	private async Task<bool> Login(CancellationToken ct = default)
	{
		if (_registerPassword is null || _registerUsername is null) return true;

		var credentials = new LoginRequest
		{
			Password = _registerPassword,
			Username = _registerUsername
		};

		var request = new HttpRequestMessage(HttpMethod.Post, )
		{
			Content = new StringContent(JsonSerializer.Serialize(credentials, Constants.SerializerOptions))
			{
				Headers = { ContentType = new("application/json") }
			}
		};

		try
		{
			var response = await _httpClient.SendAsync(request, ct);

			var content = await response.Content.ReadAsStringAsync(ct);

			var parsedResponse = JsonSerializer.Deserialize<LoginResponse>(content, Constants.SerializerOptions);

			if (parsedResponse is null) return false;

			if (!_httpClientHandler.CookieContainer.GetAllCookies().Any()) return false;

			foreach (Cookie cookie in _httpClientHandler.CookieContainer.GetAllCookies())
			{
				_cookieExpiration = _cookieExpiration > cookie.Expires
					? _cookieExpiration
					: cookie.Expires;
			}

			_loggedIn = parsedResponse.LoggedIn;

			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task<bool> TryLoginIfNotAlready(CancellationToken ct = default)
	{
		if (_loggedIn && DateTime.UtcNow <= _cookieExpiration)
		{
			return true;
		}
		else if (_loggedIn)
		{
			return await Login(ct);
		}
		else
		{
			return false;
		}
	}

	public async Task<List<CalendarDay>?> GetCalendar(DateTime startDate, DateTime endDate, CancellationToken ct = default)
	{
		if (!await TryLoginIfNotAlready(ct)) return null;

		return null;
	}

	private Task<ResponseWrapper<T>> PostJson<T>(Uri uri) where T : class
	{

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