using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Public course pages: browse, search, course details and reviews.
    [Route("api/courses")]
    public class CoursesController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;

        public CoursesController(AppDbContext db, CourseAccessService accessService)
        {
            _db = db;
            _accessService = accessService;
        }

        // GET api/courses?search=react&categoryId=1&level=Beginner&price=free&sort=popular&page=1&pageSize=12
        [HttpGet]
        public async Task<ActionResult<CoursePageDto>> GetCourses(
            string search, int? categoryId, string level, string price,
            string sort = "popular", int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            // Start with every published course, then add the filters one by one.
            IQueryable<Course> query = _db.Courses.Where(c => c.IsPublished);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string text = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(text) ||
                    c.Subtitle.ToLower().Contains(text) ||
                    c.Instructor.FullName.ToLower().Contains(text));
            }

            if (categoryId != null)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(c => c.Level == level);
            }

            if (price == "free")
            {
                query = query.Where(c => c.Price == 0);
            }
            else if (price == "paid")
            {
                query = query.Where(c => c.Price > 0);
            }

            // Sorting
            if (sort == "newest")
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }
            else if (sort == "rating")
            {
                query = query.OrderByDescending(c => c.Reviews.Count > 0 ? c.Reviews.Average(r => r.Rating) : 0);
            }
            else if (sort == "price-low")
            {
                query = query.OrderBy(c => c.Price);
            }
            else if (sort == "price-high")
            {
                query = query.OrderByDescending(c => c.Price);
            }
            else
            {
                // "popular" = most students first
                query = query.OrderByDescending(c => c.Enrollments.Count);
            }

            int totalCount = await query.CountAsync();

            List<CourseCardDto> items = await CourseMapper.SelectCards(query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new CoursePageDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET api/courses/5  -> everything for the course landing page
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDetailDto>> GetCourse(int id)
        {
            Course course = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            int userId = GetUserId();
            string role = GetUserRole();
            bool isOwner = course.InstructorId == userId;

            // Draft courses are only visible to their instructor and to admins.
            if (!course.IsPublished && !isOwner && role != Roles.Admin)
            {
                return ErrorMessage(404, "Course not found.");
            }

            Enrollment enrollment = null;
            bool hasActiveSubscription = false;
            if (userId != 0)
            {
                enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == id);
                hasActiveSubscription = await _accessService.HasActiveSubscriptionAsync(userId);
            }

            bool canWatch = await _accessService.CanWatchCourseAsync(userId, role, course);

            List<Review> reviews = await _db.Reviews.Where(r => r.CourseId == id).ToListAsync();
            int studentCount = await _db.Enrollments.CountAsync(e => e.CourseId == id);

            List<SectionDto> sections = CourseMapper.BuildSections(course, canWatch, new List<LectureProgress>());

            CourseDetailDto dto = new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Subtitle = course.Subtitle,
                Description = course.Description,
                WhatYouWillLearn = course.WhatYouWillLearn,
                Requirements = course.Requirements,
                Language = course.Language,
                Level = course.Level,
                Price = course.Price,
                ThumbnailUrl = course.ThumbnailUrl,
                PromoVideoUrl = course.PromoVideoUrl,
                IsPublished = course.IsPublished,
                UpdatedAt = course.UpdatedAt,
                CategoryId = course.CategoryId,
                CategoryName = course.Category != null ? course.Category.Name : null,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor.FullName,
                InstructorHeadline = course.Instructor.Headline,
                InstructorBio = course.Instructor.Bio,
                InstructorImageUrl = course.Instructor.ProfileImageUrl,
                AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
                ReviewCount = reviews.Count,
                StudentCount = studentCount,
                LectureCount = sections.Sum(s => s.Lectures.Count),
                TotalDurationSeconds = sections.Sum(s => s.Lectures.Sum(l => l.DurationSeconds)),
                Sections = sections,
                IsEnrolled = enrollment != null,
                IsOwner = isOwner,
                HasPurchased = enrollment != null && enrollment.AccessType == AccessTypes.Purchased,
                CanWatch = canWatch,
                HasActiveSubscription = hasActiveSubscription
            };

            return Ok(dto);
        }

        // GET api/courses/5/reviews
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<List<ReviewDto>>> GetReviews(int id)
        {
            List<ReviewDto> reviews = await _db.Reviews
                .Where(r => r.CourseId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.FullName,
                    UserImageUrl = r.User.ProfileImageUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // POST api/courses/5/reviews  -> add or update my review (enrolled students only)
        [Authorize]
        [HttpPost("{id}/reviews")]
        public async Task<ActionResult> SaveReview(int id, ReviewSaveRequest request)
        {
            int userId = GetUserId();

            bool isEnrolled = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == id);
            if (!isEnrolled)
            {
                return ErrorMessage(403, "Only enrolled students can review this course.");
            }

            Review review = await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == id);
            if (review == null)
            {
                review = new Review
                {
                    UserId = userId,
                    CourseId = id,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Reviews.Add(review);
            }

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Thank you for your review!" });
        }
    }
}
