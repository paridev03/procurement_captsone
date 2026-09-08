using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Services;

/// <summary>Single source of truth for "which vendor does this category suggest" — a
/// recommendation only (Procurement Admin can freely override it), so this stays a plain
/// lookup rather than a hard business rule enforced anywhere else.</summary>
public static class VendorRecommendationRules
{
    private static readonly Dictionary<Category, string> Recommendations = new()
    {
        [Category.IT_EQUIPMENT] = "TechSource",
        [Category.OFFICE_SUPPLIES] = "OfficeMart",
        [Category.TRAINING] = "EnterpriseSupply",
    };

    public static string? Recommend(Category category) =>
        Recommendations.TryGetValue(category, out var vendor) ? vendor : null;
}
