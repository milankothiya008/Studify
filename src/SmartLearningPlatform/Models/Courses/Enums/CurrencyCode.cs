using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Courses.Enums;

/// <summary>
/// ISO-4217 currency of a <see cref="CoursePricing"/> row. Collapsed from the
/// Java <c>currency</c> lookup table (name + 3-letter code).
/// </summary>
public enum CurrencyCode
{
    [Display(Name = "USD — US Dollar")] USD,
    [Display(Name = "EUR — Euro")] EUR,
    [Display(Name = "GBP — Pound Sterling")] GBP,
    [Display(Name = "INR — Indian Rupee")] INR,
    [Display(Name = "CAD — Canadian Dollar")] CAD,
    [Display(Name = "AUD — Australian Dollar")] AUD,
    [Display(Name = "JPY — Japanese Yen")] JPY,
}

public static class CurrencyCodeExtensions
{
    /// <summary>Symbol used when rendering a price in the views.</summary>
    public static string Symbol(this CurrencyCode code) => code switch
    {
        CurrencyCode.USD or CurrencyCode.CAD or CurrencyCode.AUD => "$",
        CurrencyCode.EUR => "€",
        CurrencyCode.GBP => "£",
        CurrencyCode.INR => "₹",
        CurrencyCode.JPY => "¥",
        _ => string.Empty,
    };

    public static string Format(this CurrencyCode code, decimal amount) =>
        $"{code.Symbol()}{amount:N2} {code}";
}
