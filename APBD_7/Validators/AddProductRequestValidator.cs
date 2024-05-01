using APBD_7.DTOs;
using FluentValidation;

namespace APBD_7.Validators;

public class AddProductRequestValidator : AbstractValidator<AddProductRequestDTO>
{
    public AddProductRequestValidator()
    {
        RuleFor(p => p.Amount).GreaterThan(0);
    }
}