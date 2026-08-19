using Microsoft.Extensions.Compliance.Classification;

namespace CleanArch.Api.Logging;

internal static class FinancialDataClassifications
{
    private const string Taxonomy = "FinancialApi";

    internal static DataClassification Personal => new(Taxonomy, nameof(Personal));
    internal static DataClassification Financial => new(Taxonomy, nameof(Financial));
    internal static DataClassification Secret => new(Taxonomy, nameof(Secret));
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class PersonalDataAttribute()
    : DataClassificationAttribute(FinancialDataClassifications.Personal);

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class FinancialDataAttribute()
    : DataClassificationAttribute(FinancialDataClassifications.Financial);

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class SecretDataAttribute()
    : DataClassificationAttribute(FinancialDataClassifications.Secret);
