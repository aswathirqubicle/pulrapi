using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Dashboard.Application.Mediatr.Users.Users;

namespace Dashboard.Application.Mediatr.Users.Queries
{
    public class GetUserQuery : IRequest<UserDetailsResponse>
    {
        [Required] public string? Uid { get; set; }
    }

    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDetailsResponse>
    {
        private readonly ILogger<GetUserQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetUserQueryHandler(ILogger<GetUserQueryHandler> logger, IApplicationDbContext dbContext, IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<UserDetailsResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _dbContext.Users.Where(e => e.Id == request.Uid)
                    .ProjectTo<UserDetailsResponse>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);
               
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                user.ProfileImageUrl = await _dbContext.Profiles.Where(p => p.UserId == request.Uid).Select(p => p.ImageUrl)
                    .SingleOrDefaultAsync(cancellationToken) ?? "";
                
                return user;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}