namespace SmartLearning.Api.Dtos
{
    // One course on the "My learning" page.
    public class MyCourseDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
        public string InstructorName { get; set; }
        public string AccessType { get; set; }
        public decimal Price { get; set; }
        public int TotalLectures { get; set; }
        public int CompletedLectures { get; set; }
        public int ProgressPercent { get; set; }
        public int? LastLectureId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // False when the course was opened with a subscription that has expired.
        public bool CanWatch { get; set; }
    }

    // Everything the course player page needs.
    public class PlayerDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
        public string Description { get; set; }
        public List<SectionDto> Sections { get; set; }
        public int TotalLectures { get; set; }
        public int CompletedLectures { get; set; }
        public int ProgressPercent { get; set; }
        public int? LastLectureId { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsCourseCompleted { get; set; }
    }

    public class ProgressRequest
    {
        // Current position in the video, in seconds.
        [System.ComponentModel.DataAnnotations.Range(0, 86400)]
        public int WatchedSeconds { get; set; }
    }

    public class CompleteRequest
    {
        public bool IsCompleted { get; set; }
    }

    // Sent back after saving progress, so the page can update the progress bar.
    public class ProgressResponse
    {
        public int LectureId { get; set; }
        public bool IsLectureCompleted { get; set; }
        public int TotalLectures { get; set; }
        public int CompletedLectures { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsCourseCompleted { get; set; }
    }

    public class CertificateDto
    {
        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public string InstructorName { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime CompletedAt { get; set; }
        public string CertificateNumber { get; set; }
    }
}
