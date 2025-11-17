using Azure.Security.KeyVault.Secrets;

namespace examScheduler.Services;

public interface IKeyVaultService
{
	public Task<string?> GetAsync(string key, CancellationToken ct);
	public string? Get(string key);
}

public class KeyVaultService : IKeyVaultService
{
	private readonly SecretClient? _client;

	public KeyVaultService(SecretClient client) => _client = client;
	public KeyVaultService() { }

	public string? Get(string key)
	{
		try
		{
			return _client?.GetSecret(key).Value.Value;
		}
		catch
		{
			return null;
		}
	}

	public async Task<string?> GetAsync(string key, CancellationToken ct = default)
	{
		try
		{
			if (_client is null) { return null; }
			return ( await _client.GetSecretAsync(key, cancellationToken: ct)).Value.Value;
		}
		catch
		{
			return null;
		}
	}
}
