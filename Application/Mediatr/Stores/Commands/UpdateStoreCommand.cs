using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Security.Validation.Attributes;
using Core.Application.Constants;
using Core.Application.Models.Stores;
using Core.Application.Mediatr.Stores.Queries;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Stores.Commands;

public class UpdateStoreCommand : IRequest<StoreDetailsResponse>
{
    [SafeUid(allowNullValue: false, ErrorMessage = "Store UID contains invalid characters or format.")]
    public string Uid { get; set; }

    [SafeName(maxLength: 100, minLength: 1, allowNullValue: true, ErrorMessage = "Store name contains invalid characters or format.")]
    public string Name { get; set; }

    [SafeName(maxLength: 200, minLength: 1, allowNullValue: true, ErrorMessage = "Legal name contains invalid characters or format.")]
    public string LegalName { get; set; }

    [SafeName(maxLength: 1000, minLength: 1, allowNullValue: true, ErrorMessage = "About field contains invalid characters or format.")]
    public string About { get; set; }

    [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "Unique name contains invalid characters or format.")]
    public string UniqueName { get; set; }
    
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string StoreEmail { get; set; }
    public bool IsEmailPublic { get; set; }
    
    [SafeUid(allowNullValue: true, ErrorMessage = "Currency UID contains invalid characters or format.")]
    public string CurrencyUid { get; set; }
    public string Description { get; set; }
    public string AffiliateId { get; set; }
    public string Location { get; set; }
    public string PhoneNumber { get; set; }
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
}

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, StoreDetailsResponse>
{
    private readonly ILogger<UpdateStoreCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMediator _mediator;

    public UpdateStoreCommandHandler(
        ILogger<UpdateStoreCommandHandler> logger,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        IMediator mediator)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<StoreDetailsResponse> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var uniqueName = UsernameHelper.Normalize(request.UniqueName);
            var isAdmin = _currentUserService.HasRole(PulrRoles.Administrator);

            var store = await _dbContext.Stores
                .Include(s => s.Affiliate)
                .Include(s => s.StoreSocialMedia)
                .Where(s => (isAdmin || s.User.Id == _currentUserService.GetUserId()) && s.Uid == request.Uid)
                .SingleOrDefaultAsync(cancellationToken);

            if (!String.IsNullOrWhiteSpace(request.AffiliateId))
            {
                store.Affiliate = await _dbContext.Affiliates.SingleOrDefaultAsync(a => a.AffiliateId == request.AffiliateId);
            }

            if (!String.IsNullOrWhiteSpace(request.Name))
            {
                store.Name = request.Name ?? store.Name;
            }

            if (!String.IsNullOrWhiteSpace(request.LegalName))
            {
                store.LegalName = request.LegalName;
            }

            if (!String.IsNullOrWhiteSpace(uniqueName))
            {
                store.UniqueName = uniqueName;
            }

            if (!String.IsNullOrWhiteSpace(request.StoreEmail))
            {
                store.StoreEmail = request.StoreEmail;
            }

            store.IsEmailPublic = request.IsEmailPublic;

            if (!String.IsNullOrWhiteSpace(request.CurrencyUid))
            {
                store.Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == request.CurrencyUid,
                    cancellationToken);
            }

            if (!String.IsNullOrWhiteSpace(request.Description))
            {
                store.Description = request.Description;
            }

            store.About = request.About;
            store.Location = request.Location;
            store.PhoneNumber = request.PhoneNumber;

            if (store.StoreSocialMedia is null)
                store.StoreSocialMedia = new StoreSocialMedia();

            store.StoreSocialMedia.WebsiteUrl = request.WebsiteUrl;
            store.StoreSocialMedia.FacebookUrl = request.FacebookUrl;
            store.StoreSocialMedia.InstagramUrl = request.InstagramUrl;
            store.StoreSocialMedia.TwitterUrl = request.TwitterUrl;
            store.StoreSocialMedia.TikTokUrl = request.TikTokUrl;
            store.StoreSocialMedia.TikTokUrl = request.TikTokUrl;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await _mediator.Send(new GetStoreDetailsQuery() { Uid = store.Uid });
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}