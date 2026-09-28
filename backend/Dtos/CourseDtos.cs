using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    // Small version of a course, used for the course cards in lists.
    public class CourseCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ThumbnailUrl { get; set; }
        public string InstructorName { get; set; }
        public string CategoryName { get; set; }
        public string Level { get; set; }
        public decimal Price { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int StudentCount { get; set; }
        public int LectureCount { get; set; }
        public int TotalDurationSeconds { get; set; }
        public bool IsPublished { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // One page of course cards + how many courses exist in total.
    public class CoursePageDto
    {
        public List<CourseCardDto> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class LectureDto
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsFreePreview { get; set; }
        public int OrderIndex { get; set; }

        // Null when the current user is not allowed to watch this lecture.
        public string VideoUrl { get; set; }

        // True when the lecture has a video (even if the link is hidden).
        public bool HasVideo { get; set; }

        // Progress of the current user (only used in the course player).
        public bool IsCompleted { get; set; }
        public int WatchedSeconds { get; set; }
    }

    public class SectionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public List<LectureDto> Lectures { get; set; }

        // The quiz at the end of the section, or null.
        public QuizInfoDto Quiz { get; set; }
    }

    // Everything the course landing page needs.
    public class CourseDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string WhatYouWillLearn { get; set; }
        public string Requirements { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; }
        public string PromoVideoUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
        public string InstructorHeadline { get; set; }
        public string InstructorBio { get; set; }
        public string InstructorImageUrl { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int StudentCount { get; set; }
        public int LectureCount { get; set; }
        public int TotalDurationSeconds { get; set; }

        public List<SectionDto> Sections { get; set; }

        // Information about the logged in user and this course.
        public bool IsEnrolled { get; set; }
        public bool IsOwner { get; set; }
        public bool HasPurchased { get; set; }
        public bool CanWatch { get; set; }
        public bool HasActiveSubscription { get; set; }
    }

    // Used by instructors to create or edit a course.
    public class CourseSaveRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(300)]
        public string Subtitle { get; set; }

        public string Description { get; set; }
        public string WhatYouWillLearn { get; set; }
        public string Requirements { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }

        [Range(0, 1000000)]
        public decimal Price { get; set; }

        public int? CategoryId { get; set; }
    }

    public class SectionSaveRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
    }

    public class LectureSaveRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsFreePreview { get; set; }
    }

    // One row on the instructor dashboard.
    public class InstructorCourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsPublished { get; set; }
        public decimal Price { get; set; }
        public int StudentCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal Revenue { get; set; }
        public int LectureCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InstructorDashboardDto
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public List<InstructorCourseDto> Courses { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CourseCount { get; set; }
    }

    public class CategorySaveRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserImageUrl { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewSaveRequest
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string Comment { get; set; }
    }
}
