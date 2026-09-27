using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Courses;

/// <summary>
/// Artwork and media for a course. Port of the Java <c>CourseMedia</c>
/// (<c>course_media</c>). The Java side held three FKs into a Cloudinary-backed
/// <c>files</c> table; with no File entity in the requested model list each one
/// becomes the URL it resolved to.
/// </summary>
[Table("course_media")]
public class CourseMedia : DateAudit
{
    [Key]
    [Display(Name = "Course")]
    public long CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [Url(ErrorMessage = "Enter a valid URL.")]
    [StringLength(1000)]
    [Display(Name = "Thumbnail URL")]
    public string? ThumbnailUrl { get; set; }

    [Url(ErrorMessage = "Enter a valid URL.")]
    [StringLength(1000)]
    [Display(Name = "Promotional video URL")]
    public string? PromotionalLessonUrl { get; set; }

    [Url(ErrorMessage = "Enter a valid URL.")]
    [StringLength(1000)]
    [Display(Name = "Certificate template URL")]
    public string? CertificateTemplateUrl { get; set; }

    [NotMapped]
    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);
}
