using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Commands.WriteOffInventory;
using VetFlow.Domain.Common;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Body of POST /api/v1/inventory/write-offs (REQ-INV-011). There is no direction: a write-off only
/// removes. The reason arrives as a string and is parsed here rather than bound as an enum, so an
/// unknown value becomes the canonical validation shape instead of a deserialization failure
/// (STD-API-010/014/023) — the same correction the adjustment endpoint needed.
/// </summary>
public sealed record WriteOffInventoryRequest(
    Guid BatchId,
    decimal Quantity,
    string? Reason,
    string? ReasonNote,
    string? ActorName)
{
    /// <summary>
    /// The write-off list, and only it. «تصحيح جرد» · «رصيد افتتاحيّ» · «موجود» are absent by
    /// design: they belong to adjustments (DEC-INV-031), and «موجود» on a write-off is a
    /// contradiction.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, WriteOffReason> Reasons =
        new Dictionary<string, WriteOffReason>(StringComparer.OrdinalIgnoreCase)
        {
            ["expired"] = WriteOffReason.Expired,
            ["damaged"] = WriteOffReason.Damaged,
            ["lost"] = WriteOffReason.Lost,
            ["contaminated"] = WriteOffReason.Contaminated,
            ["other"] = WriteOffReason.Other,
        };

    public WriteOffInventoryCommand ToCommand()
    {
        if (Reason is null || !Reasons.TryGetValue(Reason.Trim(), out var reason))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = [ValidationMessageKeys.AdjustmentReasonRequired],
            });
        }

        return new WriteOffInventoryCommand
        {
            BatchId = BatchId,
            Quantity = Quantity,
            Reason = reason,
            ReasonNote = string.IsNullOrWhiteSpace(ReasonNote) ? null : ReasonNote.Trim(),
            ActorName = string.IsNullOrWhiteSpace(ActorName) ? null : ActorName.Trim(),
        };
    }
}
