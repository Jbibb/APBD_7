using APBD_7.DTOs;
using FluentValidation;

namespace APBD_7.Validators;

public class OrderDateValidator : AbstractValidator<OrderAndRequestCreatedAtDTO>
{
    public OrderDateValidator()
    {
        RuleFor(o => o.OrderCreatedAt < o.RequestCreatedAt);
    }
}