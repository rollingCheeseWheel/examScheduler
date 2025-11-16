using examScheduler.Data;

namespace examScheduler.Services;

public interface IKeyVaultService
{
	public string? GetSecret(string schoolId);
}

public class KeyVaultService : IKeyVaultService
{
	public string? GetSecret(string schoolId)
	{
		throw new NotImplementedException();
	}
}
