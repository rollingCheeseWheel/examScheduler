using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace examScheduler.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[ ] args)
	{
		DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
			.UseNpgsql()
			.Options;
		return new AppDbContext(options);
	}
}
