using FluentValidation;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
    {
        public CreatePostCommandValidator()
        {
            RuleFor(x => x.MediaFileUid)
                .NotEmpty()
                .WithMessage("Media file is required");
        }
    }
}


