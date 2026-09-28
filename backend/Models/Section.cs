namespace SmartLearning.Api.Models
{
    // A course is split into sections, and every section has lectures.
    public class Section
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public string Title { get; set; }

        // Position of the section inside the course (1, 2, 3 ...)
        public int OrderIndex { get; set; }

        public List<Lecture> Lectures { get; set; } = new List<Lecture>();

        // Optional quiz at the end of the section.
        public Quiz Quiz { get; set; }
    }
}
