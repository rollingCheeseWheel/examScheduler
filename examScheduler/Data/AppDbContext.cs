using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}


}
