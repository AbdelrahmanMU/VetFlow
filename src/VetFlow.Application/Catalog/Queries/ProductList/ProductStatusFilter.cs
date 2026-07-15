namespace VetFlow.Application.Catalog.Queries.ProductList;

/// <summary>The active/disabled list filter (BR-CAT-045); absent means both.</summary>
public enum ProductStatusFilter
{
    Active = 1,
    Disabled = 2,
}
