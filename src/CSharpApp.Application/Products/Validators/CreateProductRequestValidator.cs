namespace CSharpApp.Application.Products.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty();

        RuleFor(request => request.Price)
            .NotNull()
            .GreaterThan(0);

        RuleFor(request => request.Description)
            .NotEmpty();

        RuleFor(request => request.CategoryId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(request => request.Images)
            .NotEmpty();
    }
}
