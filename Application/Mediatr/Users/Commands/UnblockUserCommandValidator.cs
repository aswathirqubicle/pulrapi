using FluentValidation;
using System;

namespace Core.Application.Mediatr.Users.Commands
{
	public class UnblockUserCommandValidator : AbstractValidator<UnblockUserCommand>
	{
		public UnblockUserCommandValidator()
		{
			RuleFor(x => x.ProfileIdToUnblock)
				.NotEmpty().WithMessage("profileIdToUnblock is required")
				.Must(BeValidGuid).WithMessage("Invalid UID format");
		}

		private bool BeValidGuid(string value)
		{
			return Guid.TryParse(value, out _);
		}
	}
}

