using FluentValidation;
using System;

namespace Core.Application.Mediatr.Users.Commands
{
	public class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
	{
		public BlockUserCommandValidator()
		{
			RuleFor(x => x.ProfileIdToBlock)
				.NotEmpty().WithMessage("profileIdToBlock is required")
				.Must(BeValidGuid).WithMessage("Invalid UID format");
		}

		private bool BeValidGuid(string value)
		{
			return Guid.TryParse(value, out _);
		}
	}
}

