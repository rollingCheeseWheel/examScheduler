using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Util;

namespace examScheduler.Services;

public interface IStudentService
{
    Task<StudentProfile?> GetStudentProfileAsync(Guid id, CancellationToken ct);
}

public class StudentService(AppDbContext context) : IStudentService
{
    private readonly AppDbContext _context = context;

    public async Task<StudentProfile?> GetStudentProfileAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.StudentProfiles
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

}
