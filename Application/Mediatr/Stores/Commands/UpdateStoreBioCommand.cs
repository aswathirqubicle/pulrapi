using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Commands;

public class UpdateStoreBioCommand : IRequest<StoreBioDto>
{
    public string Uid { get; set; }
    public string UniqueName { get; set; }
    public string About { get; set; }
    public string Location { get; set; }
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
}

public class UpdateStoreBioCommandHandler : IRequestHandler<UpdateStoreBioCommand, StoreBioDto>
{
    private readonly ILogger<UpdateStoreBioCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public UpdateStoreBioCommandHandler(ILogger<UpdateStoreBioCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<StoreBioDto> Handle(UpdateStoreBioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            var store = await _dbContext.Stores
                .Where(s => s.Uid == request.Uid)
                .SingleOrDefaultAsync(cancellationToken);


            store.StoreSocialMedia = await _dbContext.StoreSocialMedias.SingleOrDefaultAsync(sm => sm.StoreId == store.Id) ?? new StoreSocialMedia();

            store.UniqueName = request.UniqueName;
            store.About = request.About;
            store.Location = request.Location;
            store.UniqueName = request.UniqueName;
            store.StoreSocialMedia.WebsiteUrl = request.WebsiteUrl;
            store.StoreSocialMedia.FacebookUrl = request.FacebookUrl;
            store.StoreSocialMedia.InstagramUrl = request.InstagramUrl;
            store.StoreSocialMedia.TwitterUrl = request.TwitterUrl;
            store.StoreSocialMedia.TikTokUrl = request.TikTokUrl;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var x = _mapper.Map<StoreBioDto>(store);
            return x;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating short bio for store");
            throw;
        }
    }
}