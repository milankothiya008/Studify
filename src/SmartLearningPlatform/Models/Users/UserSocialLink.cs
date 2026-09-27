using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// One outbound profile link. Port of the Java <c>UserSocialLink</c>
/// (<c>user_social_links</c>); the <c>platform_id</c> FK is now the
/// <see cref="SocialPlatform"/> enum.
/// </summary>
[Table("user_social_links")]
public class UserSocialLink : ITimestampAudit, ISoftDeletable
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Select a user.")]
    [Display(Name = "User")]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required(ErrorMessage = "Select a platform.")]
    [Display(Name = "Platform")]
    public SocialPlatform Platform { get; set; }

    [Required(ErrorMessage = "URL is required.")]
    [Url(ErrorMessage = "Enter a valid URL, including https://")]
    [StringLength(1000)]
    [Display(Name = "URL")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Created at")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    public DateTime UpdatedAt { get; set; }

    [Display(Name = "Deleted at")]
    public DateTime? DeletedAt { get; set; }
}
