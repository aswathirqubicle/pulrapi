using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Country.Queries;
using Core.Application.Models.Country;

namespace Core.Application.Mediatr.Country.Queries
{
    public class GetCitiesByCountryQuery : IRequest<List<CityResponse>>
    {
        public string CountryUid { get; set; }
    }

    public class GetCitiesByCountryQueryHandler : IRequestHandler<GetCitiesByCountryQuery, List<CityResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetCitiesByCountryQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<CityResponse>> Handle(GetCitiesByCountryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //return await _dbContext.Cities.Select(c => new CityResponse()
                //{
                //    Name = c.Name,
                //    Uid = c.Uid,
                //    CountryUid = c.Country.Uid
                //}).ToListAsync();
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting cities by country: {e.Message}", e);
            }
        }
    }
}
