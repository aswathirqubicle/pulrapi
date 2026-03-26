using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Events;
using Core.Application.Models.Stores;
using AutoMapper;
using Core.Application.Mediatr.Stores.Queries;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class CreateStoreCommand : IRequest<StoreDetailsResponse>
    {
        [SafeName(allowNullValue: false, maxLength: 100, minLength: 1, ErrorMessage = "Store name contains invalid characters or format.")]
        public string Name { get; set; }

        [SafeName(allowNullValue: false, maxLength: 200, minLength: 1, ErrorMessage = "Legal name contains invalid characters or format.")]
        public string LegalName { get; set; }

        [SafeName(allowNullValue: false, maxLength: 50, minLength: 1, ErrorMessage = "Unique name contains invalid characters or format.")]
        public string UniqueName { get; set; }
        
        [Required(ErrorMessage = "Store email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string StoreEmail { get; set; }
        
        [SafeUid(allowNullValue: false, ErrorMessage = "Currency UID contains invalid characters or format.")]
        public string CurrencyUid { get; set; }
    }

    public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, StoreDetailsResponse>
    {
        private readonly ILogger<CreateStoreCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMediator _mediator;

        public CreateStoreCommandHandler(
            ILogger<CreateStoreCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IMediator mediator)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mediator = mediator;
        }

        public async Task<StoreDetailsResponse> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var uniqueName = UsernameHelper.Normalize(request.UniqueName);

                var user = await _currentUserService.GetUserAsync();
                if (user == null)
                {
                    throw new BadRequestException("User doesnt exist.");
                }

                var store = new Store
                {
                    Name = request.Name,
                    LegalName = request.LegalName,
                    UniqueName = uniqueName,
                    StoreEmail = request.StoreEmail,
                    Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == request.CurrencyUid, cancellationToken)
                };

                store.User = user;
                
                store.AddDomainEvent(new StoreCreatedEvent(store));
                
                _dbContext.Stores.Add(store);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return await _mediator.Send(new GetStoreByNameQuery() { UniqueName = uniqueName });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
