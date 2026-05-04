using FluentValidation;
using VyaaparNexus.Application.DTOs;

namespace VyaaparNexus.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.State).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Pincode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(60);
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.State).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Pincode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(60);
    }
}
