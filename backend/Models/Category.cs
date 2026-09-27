namespace SmartLearning.Api.Models
{
    // e.g. "Development", "Business", "Design"
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<Course> Courses { get; set; } = new List<Course>();
    }
}
