namespace SmartLearning.Api.Models
{
    public class Lecture
    {
        public int Id { get; set; }

        public int SectionId { get; set; }
        public Section Section { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public string VideoUrl { get; set; }

        // Id of the file in Cloudinary. We need it to delete the old video.
        public string VideoPublicId { get; set; }

        public int DurationSeconds { get; set; }

        // Anyone can watch a free preview lecture from the course page.
        public bool IsFreePreview { get; set; }

        // Position of the lecture inside its section (1, 2, 3 ...)
        public int OrderIndex { get; set; }
    }
}
