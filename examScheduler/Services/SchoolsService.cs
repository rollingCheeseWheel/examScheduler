using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface ISchoolsService
{
	Task<IEnumerable<School>> GetSchoolsAsync_AsNoTracking(CancellationToken ct = default);
	Task<School?> GetSchoolBySchoolIdAsync_AsNoTracking(string schoolId, CancellationToken ct = default);
}

public class SchoolsService(AppDbContext context) : ISchoolsService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<School>> GetSchoolsAsync_AsNoTracking(CancellationToken ct = default) => await _context.Schools
			.AsNoTracking()
			.ToListAsync(ct);

	public async Task<School?> GetSchoolBySchoolIdAsync_AsNoTracking(string schoolId, CancellationToken ct = default) => await _context.Schools
		.AsNoTracking()
		.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
}
