using Entities;
using examScheduler.Data;

namespace examScheduler.Services;

public interface IStudentService
{
	Task<StudentProfile?> GetStudentProfileAsync(Guid id, CancellationToken ct);
}

public class StudentService(AppDbContext context) : IStudentService
{
	private readonly AppDbContext _context = context;

	public async Task<StudentProfile?> GetStudentProfileAsync(Guid id, CancellationToken ct = default) => await _context.StudentProfiles.FindAsync([ id ], ct);

}
