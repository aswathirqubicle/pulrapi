using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Country.Queries;
using Core.Application.Models.Country;

namespace Core.Application.Mediatr.Country.Queries
{
    public class GetCountriesQuery : IRequest<List<CountryResponse>> { }

    public class GetCountriesQueryHandler : IRequestHandler<GetCountriesQuery, List<CountryResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetCountriesQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CountryResponse>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.Countries.Take(1000).Select(c => new CountryResponse()
                {
                    Name = c.Name,
                    Uid = c.Uid,
                    Iso2 = c.Iso2,
                    Iso3 = c.Iso3,
                    Iso4 = c.Iso4
                }).ToListAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting countries: {e.Message}", e);
            }
        }
    }
}
