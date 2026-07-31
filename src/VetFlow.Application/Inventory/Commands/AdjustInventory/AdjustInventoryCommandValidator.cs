using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Commands.AdjustInventory;

/// <summary>
/// Shape validation for an adjustment (STD-BE-027). Each field raises its own key so the form
/// highlights exactly what is wrong (AC-INV-051..054).
///
/// <para>What is deliberately <b>not</b> here: whether the reason belongs to the adjustment list,
/// and whether the batch can absorb the decrease. Those are business rules (BR-INV-067,
/// BR-INV-061) with their own error codes and their own rejections — a validator that duplicated
/// them would give the same failure two different shapes.</para>
/// </summary>
public sealed class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(command => command.BatchId)
            .NotEmpty()
            .WithMessage(ValidationMessageKeys.AdjustmentBatchRequired);

        RuleFor(command => command.Direction)
            .IsInEnum()
            .WithMessage(ValidationMessageKeys.AdjustmentDirectionRequired);

        // Positive magnitude only: the direction carries the sign (BR-INV-064). The value itself is
        // never rounded — the stock unit is the smallest measurable one (BR-INV-058, BR-CAT-020).
        RuleFor(command => command.Quantity)
            .GreaterThan(0m)
            .WithMessage(ValidationMessageKeys.AdjustmentQuantityPositive);

        RuleFor(command => command.Reason)
            .IsInEnum()
            .WithMessage(ValidationMessageKeys.AdjustmentReasonRequired);

        RuleFor(command => command.ReasonNote)
            .MaximumLength(AdjustInventoryCommand.MaxReasonNoteLength)
            .WithMessage(ValidationMessageKeys.TextTooLong);

        // Free text, never validated for identity — there is no users module (DEC-INV-030). The
        // only constraint is a storage bound, and its absence is always acceptable (BR-INV-066).
        RuleFor(command => command.ActorName)
            .MaximumLength(AdjustInventoryCommand.MaxActorNameLength)
            .WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
