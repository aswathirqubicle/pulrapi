using Core.Application.Mediatr.Fulfillments.Commands;
using Core.Application.Models.Fulfillments;

namespace Core.Application.Mappings
{
    public class FulfillmentProfile : AutoMapper.Profile
    {
        public FulfillmentProfile()
        {
            CreateMap<CreateFulfillmentCommand, FulfillmentDetailsResponse>();
            CreateMap<UpdateFulfillmentCommand, FulfillmentDetailsResponse>();
        }
    }
}
