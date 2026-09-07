using LMSFinal.Domain.Entities;
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

    public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
    {
        public CertificateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Certificate>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            await Context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course).ThenInclude(course => course.Translations)
                .Include(c => c.Course).ThenInclude(course => course.Instructor)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.IssuedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default) =>
            await Context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course).ThenInclude(course => course.Translations)
                .Include(c => c.Course).ThenInclude(course => course.Instructor)
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, cancellationToken);

        public async Task<Certificate?> GetWithDetailsAsync(Guid certificateId, CancellationToken cancellationToken = default) =>
            await Context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course).ThenInclude(course => course.Translations)
                .Include(c => c.Course).ThenInclude(course => course.Instructor)
                .FirstOrDefaultAsync(c => c.Id == certificateId, cancellationToken);

        public async Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Certificates.AnyAsync(c => c.StudentId == studentId && c.CourseId == courseId, cancellationToken);

        public async Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default) =>
            await Context.Certificates.CountAsync(c => c.IssuedAt.Year == year, cancellationToken);
    }

}
