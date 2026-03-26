using Dashboard.Application.Mediatr.Users.Commands.Update;
using FluentValidation;

namespace Dashboard.Application.Mediatr.Users.Commands.Update;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        //RuleFor(e => e)
        //    .NotEmpty().WithMessage("Uid can't be empty.");
    }

}