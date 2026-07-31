using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;

/// <summary>
/// Front-line validation of the create-return request (REQ-PUR-006, AC-PUR-019): the originating
/// invoice and the return date are required, each producing its own field-keyed error. Whether
/// that invoice is <b>Received</b> is a business rule, not a validation key — it raises
/// VTF-PUR-015 (BR-PUR-015). Messages are resource keys; the API middleware is the single
/// translation point, and the aggregate re-enforces everything as the backstop (STD-BE-010).
/// </summary>
public sealed class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    private const int NotesMaxLength = 2000;

    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(command => command.PurchaseInvoiceId)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnOriginalInvoiceRequired)
            .NotEqual(Guid.Empty).WithMessage(ValidationMessageKeys.ReturnOriginalInvoiceRequired);

        RuleFor(command => command.ReturnDate)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnDateRequired);

        RuleFor(command => command.Notes)
            .MaximumLength(NotesMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
