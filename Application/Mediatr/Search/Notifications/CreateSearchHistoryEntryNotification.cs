using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Search.Notifications;

public class CreateSearchHistoryEntryNotification : INotification
{
    public string Term { get; set; }
}

public class CreateSearchHistoryEntryNotificationHandler : INotificationHandler<CreateSearchHistoryEntryNotification>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateSearchHistoryEntryNotificationHandler> _logger;

    public CreateSearchHistoryEntryNotificationHandler(IApplicationDbContext dbContext,
        ICurrentUserService currentUserService, ILogger<CreateSearchHistoryEntryNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(CreateSearchHistoryEntryNotification request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync();

            if (user == null)
                throw new NotAuthenticatedException();

            var term = await _dbContext.SearchHistories
                .SingleOrDefaultAsync(s => s.IsActive
                                           && s.Term.ToLower() == request.Term.Trim().ToLower()
                                           && s.UserId == user.Id
                    , cancellationToken);

            if (term != null)
            {
                term.SearchCount += 1;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            _dbContext.SearchHistories.Add(new SearchHistory
            {
                UserId = user.Id,
                Term = request.Term.Trim(),
                SearchCount = 1
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating new search history term");
            throw;
        }
    }
}