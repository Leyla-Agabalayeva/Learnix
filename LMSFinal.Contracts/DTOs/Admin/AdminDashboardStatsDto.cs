using System.Collections.Generic;

namespace LMSFinal.Contracts.DTOs.Admin
{
    public record AdminDashboardStatsDto(
        int TotalUsers,
        int TotalStudents,
        int TotalInstructors,
        int TotalAdmins,
        int TotalCourses,
        int PublishedCourses,
        int DraftCourses,
        int ArchivedCourses,
        int TotalCategories,
        int TotalEnrollments,
        int TotalCertificatesIssued,
        // Регистрации за последние 30 дней, по дням — для графика на дашборде.
        IReadOnlyList<DailyCountDto> SignupsLast30Days);
}
