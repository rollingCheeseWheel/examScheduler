using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.API;

namespace examScheduler.Services.School;

public interface ISchoolService
{
	Task<IEnumerable<Models.API.School>> GetSchoolsAsync(CancellationToken ct);
}

public class SchoolService(AppDbContext context) : ISchoolService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<Models.API.School>> GetSchoolsAsync(CancellationToken ct = default)
	{
		return await _context.Schools
			.Select(s => new Models.API.School
			{
				Name = s.Name,
				RegisterUri = s.RegisterUri
			})
			.ToListAsync(ct);
	}
}
