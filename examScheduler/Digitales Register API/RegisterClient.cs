using System.Net;

namespace examScheduler.Digitales_Register_API;

public class RegisterClient : IDisposable
{
	private bool _disposed = false;

	private HttpClientHandler _httpClientHandler;
	private HttpClient _httpClient;
	private CookieContainer _cookieContainer = new();

	private readonly string _registerUsername;
	private readonly string _registerPassword;
	public readonly Uri RegisterURI;

	private bool _loggedIn = false;
	private DateTime _loginExpiration = default;
	private string? sessionId;

	public RegisterClient(string registerUsername, string registerPassword, Uri registerURI)
	{
		_registerUsername = registerUsername;
		_registerPassword = registerPassword;
		RegisterURI = new(registerURI.Authority);

		_httpClientHandler = new HttpClientHandler()
		{
			UseCookies = true,
			CookieContainer = _cookieContainer,
		};

		_httpClient = new HttpClient(_httpClientHandler)
		{
			BaseAddress = RegisterURI
		};
	}

	public RegisterClient(
		string registerUsername,
		string registerPassword,
		string registerURI
	) : this(registerUsername, registerPassword, new Uri(registerURI)) { }

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

	private void Login(string loginPath = "", CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
