using examScheduler.Data;
using examScheduler.Mappings;
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

	public async Task<IEnumerable<School>> GetSchoolsAsync(CancellationToken ct = default) => ( await _context.Schools
			.AsNoTracking()
			.ToListAsync(ct) )
			.Select(SchoolMappings.ToDTO);
}
