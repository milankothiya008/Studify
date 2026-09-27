using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Turns Course entities into DTOs. Used by several controllers,
    // so the code is written once here.
    public static class CourseMapper
    {
        // Converts a course query into course cards.
        // Entity Framework turns this into a single SQL query.
        public static IQueryable<CourseCardDto> SelectCards(IQueryable<Course> courses)
        {
            return courses.Select(c => new CourseCardDto
            {
                Id = c.Id,
                Title = c.Title,
                Subtitle = c.Subtitle,
                ThumbnailUrl = c.ThumbnailUrl,
                InstructorName = c.Instructor.FullName,
                CategoryName = c.Category.Name,
                Level = c.Level,
                Price = c.Price,
                AverageRating = c.Reviews.Count > 0 ? c.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = c.Reviews.Count,
                StudentCount = c.Enrollments.Count,
                LectureCount = c.Sections.SelectMany(s => s.Lectures).Count(),
                TotalDurationSeconds = c.Sections.SelectMany(s => s.Lectures).Sum(l => l.DurationSeconds),
                IsPublished = c.IsPublished,
                UpdatedAt = c.UpdatedAt
            });
        }

        // Builds the curriculum (sections + lectures) of a course.
        // The course must be loaded with .Include(c => c.Sections).ThenInclude(s => s.Lectures).
        //
        // canWatch = false hides every video link except the free previews.
        // progressList = the user's progress rows (can be an empty list).
        public static List<SectionDto> BuildSections(Course course, bool canWatch, List<LectureProgress> progressList)
        {
            List<SectionDto> sections = new List<SectionDto>();

            foreach (Section section in course.Sections.OrderBy(s => s.OrderIndex))
            {
                SectionDto sectionDto = new SectionDto
                {
                    Id = section.Id,
                    Title = section.Title,
                    OrderIndex = section.OrderIndex,
                    Lectures = new List<LectureDto>()
                };

                foreach (Lecture lecture in section.Lectures.OrderBy(l => l.OrderIndex))
                {
                    LectureProgress progress = progressList.FirstOrDefault(p => p.LectureId == lecture.Id);
                    bool showVideo = canWatch || lecture.IsFreePreview;

                    sectionDto.Lectures.Add(new LectureDto
                    {
                        Id = lecture.Id,
                        SectionId = section.Id,
                        Title = lecture.Title,
                        Description = lecture.Description,
                        DurationSeconds = lecture.DurationSeconds,
                        IsFreePreview = lecture.IsFreePreview,
                        OrderIndex = lecture.OrderIndex,
                        VideoUrl = showVideo ? lecture.VideoUrl : null,
                        HasVideo = !string.IsNullOrEmpty(lecture.VideoUrl),
                        IsCompleted = progress != null && progress.IsCompleted,
                        WatchedSeconds = progress != null ? progress.WatchedSeconds : 0
                    });
                }

                sections.Add(sectionDto);
            }

            return sections;
        }

        // e.g. 3 of 12 lectures -> 25
        public static int CalculatePercent(int completed, int total)
        {
            if (total == 0)
            {
                return 0;
            }
            return completed * 100 / total;
        }
    }
}
