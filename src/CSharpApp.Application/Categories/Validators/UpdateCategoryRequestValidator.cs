namespace CSharpApp.Application.Categories.Validators;

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(request => request)
            .Must(request =>
                !string.IsNullOrWhiteSpace(request.Name) ||
                !string.IsNullOrWhiteSpace(request.Image))
            .WithMessage("At least one category field must be provided.");
    }
}
