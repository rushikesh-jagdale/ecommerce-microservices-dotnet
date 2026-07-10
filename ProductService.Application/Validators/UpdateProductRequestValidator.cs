using FluentValidation;
using ProductService.Application.DTOs;
using ProductService.Application.DTOs.ProductService.Application.DTOs;

namespace ProductService.Application.Validators
{
    public class UpdateProductRequestValidator
        : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Price)
                .GreaterThan(0);

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0);
        }
    }
}