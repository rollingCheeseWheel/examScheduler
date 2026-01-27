using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.EntityFrameworkCore;
using Models.API;

namespace examScheduler.Services;

public interface ISchoolsService
{
	Task<IEnumerable<School>> GetSchoolsAsync_AsNoTracking(CancellationToken ct = default);
	Task<Entities.School?> GetSchoolBySchoolIdAsync_AsNoTracking(string schoolId, CancellationToken ct = default);
}

public class SchoolsService(AppDbContext context) : ISchoolsService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<School>> GetSchoolsAsync_AsNoTracking(CancellationToken ct = default) => await _context.Schools
			.AsNoTracking()
			.Select(x => x.ToDTO())
			.ToListAsync(ct);

	public async Task<Entities.School?> GetSchoolBySchoolIdAsync_AsNoTracking(string schoolId, CancellationToken ct = default) => await _context.Schools
		.AsNoTracking()
		.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
}
