using MediatR;

namespace Core.Application.Mediatr.Users.Commands.AdminDelete
{
    public class AdminDeleteUserCommand : IRequest<AdminDeleteUserResponse>
    {
        public string Username { get; set; }
        public string SecretCode { get; set; }
    }

    public class AdminDeleteUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
