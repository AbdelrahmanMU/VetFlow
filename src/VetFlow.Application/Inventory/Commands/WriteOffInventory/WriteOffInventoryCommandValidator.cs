using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Commands.WriteOffInventory;

/// <summary>
/// Shape validation for a write-off (STD-BE-027). Whether the batch can absorb the removal is a
/// business rule with its own rejection (BR-INV-061), not a validation concern.
/// </summary>
public sealed class WriteOffInventoryCommandValidator : AbstractValidator<WriteOffInventoryCommand>
{
    public WriteOffInventoryCommandValidator()
    {
        RuleFor(command => command.BatchId)
            .NotEmpty()
            .WithMessage(ValidationMessageKeys.AdjustmentBatchRequired);

        RuleFor(command => command.Quantity)
            .GreaterThan(0m)
            .WithMessage(ValidationMessageKeys.AdjustmentQuantityPositive);

        RuleFor(command => command.Reason)
            .IsInEnum()
            .WithMessage(ValidationMessageKeys.AdjustmentReasonRequired);

        RuleFor(command => command.ReasonNote)
            .MaximumLength(WriteOffInventoryCommand.MaxReasonNoteLength)
            .WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.ActorName)
            .MaximumLength(WriteOffInventoryCommand.MaxActorNameLength)
            .WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
