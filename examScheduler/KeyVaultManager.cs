namespace examScheduler;

public class KeyVaultCache
{
	public IEnumerable<KeyValuePair<string, string?>> Secrets { get; private init; }

	public KeyVaultCache(string vaultConnectionString)
	{
		var configManager = new ConfigurationManager();
		configManager.AddAzureKeyVaultSecrets(vaultConnectionString);

		Secrets = configManager.AsEnumerable().Where(kvp => kvp.Value is not null)!;
	}

	public KeyValuePair<string, string?>? this[ string key ] => Secrets.FirstOrDefault(kvp => kvp.Key == key);

	public KeyValuePair<string, string?>? this[ int key ] => this[ key.ToString() ];
}

public static class KeyVaultCacheExtension
{
	public static IServiceCollection AddKeyVaultCache(this IServiceCollection services, string? keyVaultConnectionString)
	{
		if (keyVaultConnectionString is null)
		{
			return services;
		}

		return services.AddSingleton(new KeyVaultCache(keyVaultConnectionString));
	}
}
