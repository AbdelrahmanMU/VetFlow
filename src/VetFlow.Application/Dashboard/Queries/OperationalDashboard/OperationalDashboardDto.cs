using System.Text.Json.Serialization;
using VetFlow.Application.Common;

namespace VetFlow.Application.Dashboard.Queries.OperationalDashboard;

/// <summary>
/// The operational dashboard response (REQ-DSH-010). One request carries every section, and
/// <b>each section carries its own outcome</b>.
/// <para>
/// <b>Why the shape looks like this.</b> The commission asked for a single endpoint <i>and</i>
/// for retry states, which cannot both hold if the response fails as a block. So the request
/// is one and the <i>sections</i> are independent (DEC-DSH-002): a section that could not be
/// read is marked failed and the rest of the board still renders. The precedent is recorded —
/// the product-details stock card was deliberately made to load independently so "a stock
/// outage surfaces a retry in one card instead of blanking a page whose other five loaded".
/// </para>
/// </summary>
public sealed record OperationalDashboardDto
{
    /// <summary>
    /// The clinic local date the server used (BR-DSH-003, <c>clinic-date.md</c>).
    /// <para>
    /// <b>Returned so the client never derives a date of its own.</b> Device time is a
    /// prohibited source, and a browser that computed "today" itself could disagree with the
    /// number beside it.
    /// </para>
    /// </summary>
    public required DateOnly ClinicDate { get; init; }

    public required DashboardSectionsDto Sections { get; init; }
}

/// <summary>
/// The seven sections, by fixed key. <b>A section is never omitted</b> — an absent key and a
/// zero would be indistinguishable to the client, which is exactly the confusion
/// BR-DSH-014 exists to prevent.
/// </summary>
public sealed record DashboardSectionsDto
{
    public required DashboardCountSectionDto ExpiredBatches { get; init; }

    public required DashboardCountSectionDto OutOfStockProducts { get; init; }

    public required DashboardCountSectionDto ExpiringSoonBatches { get; init; }

    public required DashboardCountSectionDto DraftPurchases { get; init; }

    public required DashboardCountSectionDto DraftSales { get; init; }

    public required DashboardTodaySalesSectionDto TodaySales { get; init; }

    public required DashboardRecentMovementsSectionDto RecentMovements { get; init; }
}

/// <summary>Whether a section could be read. Serialised camel-case: <c>ok</c> / <c>failed</c>.</summary>
public enum DashboardSectionStatus
{
    /// <summary>The section was read; its data is present.</summary>
    Ok,

    /// <summary>
    /// The section could not be read. <b>Its data is absent, never zero</b> — "no expired
    /// batches" and "could not determine expired batches" are contradictory statements, and
    /// conflating them is false reassurance inside a safety decision (BR-DSH-014,
    /// DEC-INV-021).
    /// </summary>
    Failed,
}

/// <summary>A section that is a single count.</summary>
public sealed record DashboardCountSectionDto
{
    public required DashboardSectionStatus Status { get; init; }

    /// <summary>The count; <b>null when the section failed</b> — never 0 (BR-DSH-014).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }

    public static DashboardCountSectionDto Ok(int count) =>
        new() { Status = DashboardSectionStatus.Ok, Count = count };

    public static DashboardCountSectionDto Failed() =>
        new() { Status = DashboardSectionStatus.Failed };
}

/// <summary>Today's sales: a count and the dashboard's <b>only</b> money figure (DEC-DSH-012).</summary>
public sealed record DashboardTodaySalesSectionDto
{
    public required DashboardSectionStatus Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }

    /// <summary>
    /// The sum of today's committed invoice totals, as Sales produced them (DEC-SAL-004).
    /// Null when the section failed. The currency travels with the amount, so the screen
    /// assumes none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneyDto? Total { get; init; }

    public static DashboardTodaySalesSectionDto Ok(int count, MoneyDto total) =>
        new() { Status = DashboardSectionStatus.Ok, Count = count, Total = total };

    public static DashboardTodaySalesSectionDto Failed() =>
        new() { Status = DashboardSectionStatus.Failed };
}

/// <summary>The five most recent movements (BR-DSH-010).</summary>
public sealed record DashboardRecentMovementsSectionDto
{
    public required DashboardSectionStatus Status { get; init; }

    /// <summary>
    /// Up to five movements, newest first. <b>An empty list is a valid success</b> — a clinic
    /// with no movements yet is empty, not broken. Null only when the section failed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DashboardMovementDto>? Items { get; init; }

    public static DashboardRecentMovementsSectionDto Ok(IReadOnlyList<DashboardMovementDto> items) =>
        new() { Status = DashboardSectionStatus.Ok, Items = items };

    public static DashboardRecentMovementsSectionDto Failed() =>
        new() { Status = DashboardSectionStatus.Failed };
}

/// <summary>
/// One movement on the dashboard — four fields (BR-DSH-010).
/// <para>
/// <b><see cref="Type"/> is a string, not this module's enum, and that is deliberate.</b> The
/// movement-type vocabulary is a closed set owned by Inventory (BR-INV-065). Re-declaring it
/// here would put a second copy of an Inventory rule inside the Dashboard — precisely what
/// BR-DSH-001 forbids — and the two would drift the first time Inventory added a type. The
/// dashboard passes the owner's value through untouched.
/// </para>
/// </summary>
public sealed record DashboardMovementDto
{
    public required Guid MovementId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The Inventory movement type, camel-cased, passed through verbatim.</summary>
    public required string Type { get; init; }

    public required string ProductName { get; init; }

    /// <summary>Signed quantity in the product's stock unit; never rounded (BR-INV-058).</summary>
    public required decimal Quantity { get; init; }

    public required string StockUnitName { get; init; }
}
