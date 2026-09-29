namespace SmartLearning.Api.Models
{
    public class Course
    {
        public int Id { get; set; }

        public int InstructorId { get; set; }
        public User Instructor { get; set; }

        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }

        // One item per line. Example: "Build a REST API\nUse Entity Framework"
        public string WhatYouWillLearn { get; set; }
        public string Requirements { get; set; }

        public string Language { get; set; }

        // "Beginner", "Intermediate", "Advanced" or "All Levels"
        public string Level { get; set; }

        // 0 means the course is free.
        public decimal Price { get; set; }

        public string ThumbnailUrl { get; set; }
        public string ThumbnailPublicId { get; set; }

        // Short promo video shown on the course page.
        public string PromoVideoUrl { get; set; }
        public string PromoVideoPublicId { get; set; }

        // Only published courses are visible to students.
        public bool IsPublished { get; set; }

        // When the course was published for the first time. Followers of the instructor
        // are told about a new course only once, not every time it is published again.
        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<Section> Sections { get; set; } = new List<Section>();
        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public List<Review> Reviews { get; set; } = new List<Review>();
    }
}
