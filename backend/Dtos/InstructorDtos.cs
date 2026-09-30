namespace SmartLearning.Api.Dtos
{
    // An instructor with the numbers shown on cards: courses, students, rating, followers.
    public class InstructorSummaryDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Headline { get; set; }
        public string ProfileImageUrl { get; set; }

        public int CourseCount { get; set; }
        public int StudentCount { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int FollowerCount { get; set; }

        // True when the logged in user follows this instructor.
        public bool IsFollowing { get; set; }
    }

    // Everything the public instructor profile page needs.
    public class InstructorProfileDto : InstructorSummaryDto
    {
        public string Bio { get; set; }
        public string WebsiteUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
        public DateTime JoinedAt { get; set; }

        // True when the logged in user is looking at their own profile.
        public bool IsMe { get; set; }

        public List<CourseCardDto> Courses { get; set; }
        public List<InstructorReviewDto> Reviews { get; set; }
    }

    // A student review of one of the instructor's courses.
    public class InstructorReviewDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string UserImageUrl { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
    }

    // Answer of follow / unfollow, so the page can update the button and the counter.
    public class FollowResultDto
    {
        public bool IsFollowing { get; set; }
        public int FollowerCount { get; set; }
        public string Message { get; set; }
    }
}
