using APBD_7.DTOs;
using FluentValidation;
using FluentValidation.Validators;

namespace APBD_7.Validators;

public class AddProductRequestValidator : AbstractValidator<AddProductDTO>
{
    public AddProductRequestValidator()
    {
        RuleFor(p => p.Amount).GreaterThan(0);
    }
}