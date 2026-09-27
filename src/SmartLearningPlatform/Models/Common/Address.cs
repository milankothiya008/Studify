using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SmartLearningPlatform.Models.Common;

/// <summary>
/// Postal address value object. Port of the Java <c>@Embeddable Address</c>;
/// mapped as an EF Core owned type so it lands in the owner's table under
/// prefixed columns (home_street, work_street, ...) exactly as the Java
/// <c>@AttributeOverrides</c> did.
/// </summary>
[Owned]
public class Address
{
    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [Display(Name = "Postal code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Display(Name = "Full address")]
    [StringLength(500)]
    public string? FullAddress { get; set; }

    /// <summary>True when no component has been filled in.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Street)
        && string.IsNullOrWhiteSpace(City)
        && string.IsNullOrWhiteSpace(State)
        && string.IsNullOrWhiteSpace(Country)
        && string.IsNullOrWhiteSpace(PostalCode)
        && string.IsNullOrWhiteSpace(FullAddress);

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(FullAddress)) return FullAddress!;
        var parts = new[] { Street, City, State, PostalCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
