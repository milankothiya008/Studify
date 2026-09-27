using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Users.Enums;

/// <summary>
/// Which site a <see cref="UserSocialLink"/> points at. The Java model reached
/// this through a <c>platforms</c> lookup table whose only real column was
/// <c>name</c>; the requested model list has no Platform entity, so the lookup
/// collapses into this enum and the extra table disappears.
/// </summary>
public enum SocialPlatform
{
    [Display(Name = "LinkedIn")] LinkedIn,
    [Display(Name = "GitHub")] GitHub,
    [Display(Name = "X (Twitter)")] Twitter,
    [Display(Name = "Facebook")] Facebook,
    [Display(Name = "Instagram")] Instagram,
    [Display(Name = "YouTube")] YouTube,
    [Display(Name = "Personal website")] Website,
    [Display(Name = "Other")] Other,
}
