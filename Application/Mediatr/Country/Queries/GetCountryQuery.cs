using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Country.Queries;
using Core.Application.Models.Country;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Mediatr.Country.Queries
{
    public class GetCountryQuery : IRequest<CountryDetailsResponse>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class GetCountryQueryHandler : IRequestHandler<GetCountryQuery, CountryDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetCountryQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CountryDetailsResponse> Handle(GetCountryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.Countries.Where(c => c.Uid == request.Uid).Select(c => new CountryDetailsResponse()
                {
                    Name = c.Name,
                    Uid = c.Uid,
                    Iso2 = c.Iso2,
                    Iso3 = c.Iso3,
                    Iso4 = c.Iso4
                }).SingleOrDefaultAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting country: {e.Message}", e);
            }
        }
    }
}
