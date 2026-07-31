using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Commands.AdjustInventory;
using VetFlow.Domain.Common;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Body of POST /api/v1/inventory/adjustments (REQ-INV-010). The direction is explicit and the
/// quantity is a positive magnitude — the sign belongs to the domain (BR-INV-064), never to the
/// request. <c>ActorName</c> and <c>ReasonNote</c> are optional and may be absent entirely
/// (BR-INV-066/067).
///
/// <para><b>Direction and reason arrive as strings and are parsed here, not bound as enums.</b>
/// Binding them directly makes an unknown value a deserialization failure — which surfaced as a
/// 500 — instead of the canonical validation shape a client can act on (STD-API-010/014/023, the
/// <see cref="QueryStringParser"/> philosophy applied to a body). A write-off-only reason such as
/// «منتهي الصلاحية» is exactly the value a real caller will get wrong (DEC-INV-031), so it must
/// come back as a field error, not a server error.</para>
/// </summary>
public sealed record AdjustInventoryRequest(
    Guid BatchId,
    string? Direction,
    decimal Quantity,
    string? Reason,
    string? ReasonNote,
    string? ActorName)
{
    private static readonly IReadOnlyDictionary<string, AdjustmentDirection> Directions =
        new Dictionary<string, AdjustmentDirection>(StringComparer.OrdinalIgnoreCase)
        {
            ["increase"] = AdjustmentDirection.Increase,
            ["decrease"] = AdjustmentDirection.Decrease,
        };

    /// <summary>
    /// The adjustment reason list, and only it. «منتهي الصلاحية» and «ملوَّث» are absent by design:
    /// they belong to write-off (DEC-INV-031), so they are rejected here as unknown tokens rather
    /// than reaching the handler.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, AdjustmentReason> Reasons =
        new Dictionary<string, AdjustmentReason>(StringComparer.OrdinalIgnoreCase)
        {
            ["countCorrection"] = AdjustmentReason.CountCorrection,
            ["initialBalance"] = AdjustmentReason.InitialBalance,
            ["damaged"] = AdjustmentReason.Damaged,
            ["found"] = AdjustmentReason.Found,
            ["lost"] = AdjustmentReason.Lost,
            ["other"] = AdjustmentReason.Other,
        };

    public AdjustInventoryCommand ToCommand()
    {
        var errors = new Dictionary<string, string[]>();

        if (Direction is null || !Directions.TryGetValue(Direction.Trim(), out var direction))
        {
            errors["direction"] = [ValidationMessageKeys.AdjustmentDirectionRequired];
            direction = default;
        }

        if (Reason is null || !Reasons.TryGetValue(Reason.Trim(), out var reason))
        {
            errors["reason"] = [ValidationMessageKeys.AdjustmentReasonRequired];
            reason = default;
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new AdjustInventoryCommand
        {
            BatchId = BatchId,
            Direction = direction,
            Quantity = Quantity,
            Reason = reason,
            ReasonNote = string.IsNullOrWhiteSpace(ReasonNote) ? null : ReasonNote.Trim(),
            ActorName = string.IsNullOrWhiteSpace(ActorName) ? null : ActorName.Trim(),
        };
    }
}
