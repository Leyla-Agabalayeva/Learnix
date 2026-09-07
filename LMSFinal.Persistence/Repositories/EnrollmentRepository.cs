using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Repositories
{

    public class EnrollmentRepository : BaseRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<Enrollment?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
        public async Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Enrollments.AnyAsync(
                e => e.StudentId == studentId && e.CourseId == courseId && e.Status != EnrollmentStatus.Cancelled,
                cancellationToken);

        public async Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            await Context.Enrollments
                .Include(e => e.Course).ThenInclude(c => c.Translations)
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrolledAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Enrollment>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .OrderByDescending(e => e.EnrolledAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }

}
