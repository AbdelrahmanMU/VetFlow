using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CreateSalesReturn;

/// <summary>
/// Front-line validation of the create-return request (REQ-SAL-004, AC-SAL-014): the originating
/// invoice and the return date are required, each producing its own field-keyed error. Whether that
/// invoice is <b>Committed</b> is a business rule, not a validation key — it raises VTF-SAL-015
/// (BR-SAL-015). Messages are resource keys; the API middleware is the single translation point,
/// and the aggregate re-enforces everything as the backstop (STD-BE-010).
/// </summary>
public sealed class CreateSalesReturnCommandValidator : AbstractValidator<CreateSalesReturnCommand>
{
    private const int NotesMaxLength = 2000;

    public CreateSalesReturnCommandValidator()
    {
        RuleFor(command => command.SalesInvoiceId)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnOriginalInvoiceRequired)
            .NotEqual(Guid.Empty).WithMessage(ValidationMessageKeys.ReturnOriginalInvoiceRequired);

        RuleFor(command => command.ReturnDate)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnDateRequired);

        RuleFor(command => command.Notes)
            .MaximumLength(NotesMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
