using AutoMapper;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Contracts.DTOs.Wishlist;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Application.Common;

using LMSFinal.Domain.Enums;

namespace LMSFinal.Application.Services
{

    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WishlistService(
            IWishlistRepository wishlistRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _wishlistRepository = wishlistRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<WishlistItemDto>> GetMyWishlistAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var items = await _wishlistRepository.GetByStudentIdAsync(studentId, cancellationToken);

            return items
                .Select(w => new WishlistItemDto(w.Id, _mapper.Map<CourseSummaryDto>(w.Course), w.AddedAt))
                .ToList();
        }

        public async Task<IReadOnlyList<WishlistItemLocalizedDto>> GetMyWishlistLocalizedAsync(
            Guid studentId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var items = await _wishlistRepository.GetByStudentIdAsync(studentId, cancellationToken);

            // Агрегаты одним запросом на весь список — иначе рейтинг и число студентов
            // пришлось бы запрашивать для каждого курса отдельно (N+1).
            var stats = await CourseSummaryMapper.LoadStatsAsync(
                _courseRepository, items.Select(w => w.Course), cancellationToken);

            return items
                .Select(w => new WishlistItemLocalizedDto(
                    w.Id,
                    CourseSummaryMapper.ToLocalized(w.Course, language, stats.GetValueOrDefault(w.CourseId)),
                    w.AddedAt))
                .ToList();
        }

        public async Task AddAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var courseExists = await _courseRepository.ExistsAsync(courseId, cancellationToken);
            if (!courseExists)
            {
                throw new NotFoundException("Course", courseId);
            }

            var alreadyAdded = await _wishlistRepository.ExistsAsync(studentId, courseId, cancellationToken);
            if (alreadyAdded)
            {
                return; // идемпотентно
            }

            var item = new Wishlist { StudentId = studentId, CourseId = courseId };

            await _wishlistRepository.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            await _wishlistRepository.RemoveAsync(studentId, courseId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

}
