using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Mappings;
using Core.Application.Models;
using Core.Domain.Entities;
using Dashboard.Application.Mediatr.Users.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Queries
{
    public class GetUsersQuery : PagingParamsRequest, IRequest<PagingResponse<UserResponse>>
    {
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagingResponse<UserResponse>>
    {
        private readonly ILogger<GetUsersQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;

        public GetUsersQueryHandler(ILogger<GetUsersQueryHandler> logger, IApplicationDbContext dbContext, IMapper mapper, UserManager<User> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<PagingResponse<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<User> query = _dbContext.Users;

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(e =>
                        !string.IsNullOrEmpty(e.UserName) && EF.Functions.Like(e.UserName, $"%{request.Search}%") ||
                        !string.IsNullOrEmpty(e.FirstName) && EF.Functions.Like(e.FirstName, $"%{request.Search}%") ||
                        !string.IsNullOrEmpty(e.LastName) && EF.Functions.Like(e.LastName, $"%{request.Search}%"));
                }

                var result = await query
                                .Include(e => e.Profile)
                                .OrderByDescending(e => e.CreatedAt)
                                .PaginatedListAsync(request.PageNumber, request.PageSize);

                foreach (var item in result)
                {
                    await item.GetRoles(this._userManager);
                }

                var resultMapped = _mapper.Map<PagingResponse<UserResponse>>(result);

                foreach (var item in resultMapped.Items)
                {
                    item.StoresCount = await _dbContext.Stores.Where(s => s.User.Id == item.Id).CountAsync();
                }

                return resultMapped;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}