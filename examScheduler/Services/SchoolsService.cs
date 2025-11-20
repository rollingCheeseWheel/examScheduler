using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.API;

namespace examScheduler.Services;

public interface ISchoolsService
{
	Task<IEnumerable<School>> GetSchoolsAsync(CancellationToken ct);
}

public class SchoolsService(AppDbContext context) : ISchoolsService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<School>> GetSchoolsAsync(CancellationToken ct = default)
	{
		return await _context.Schools
			.Select(s => new School
			{
				Name = s.Name,
				RegisterUri = s.RegisterUri,
				ClientId = s.ClientId,
			})
			.ToListAsync(ct);
	}
}
