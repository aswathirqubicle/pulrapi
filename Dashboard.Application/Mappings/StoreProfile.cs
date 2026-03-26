using Core.Application.Models.Products;
using Core.Application.Models;
using Core.Domain.Entities;
using Dashboard.Application.Mediatr.Stores.Commands.Update;

namespace Dashboard.Application.Mappings
{
    public class StoreProfile : AutoMapper.Profile
    {
        public StoreProfile()
        {
            CreateMap<UpdateStoreCommand, Core.Application.Mediatr.Stores.Commands.UpdateStoreCommand>();
        }
    }
}