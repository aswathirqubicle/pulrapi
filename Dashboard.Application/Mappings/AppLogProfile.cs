using Core.Application.Models;
using Core.Domain.Entities;
using Dashboard.Application.Models.AppLogs;

namespace Dashboard.Application.Mappings
{
    public class AppLogProfile : AutoMapper.Profile
    {
        public AppLogProfile()
        {
            CreateMap<AppLog, AppLogResponse>();
            CreateMap<PagedList<AppLog>, PagingResponse<AppLogResponse>>().ForMember(
                                dest => dest.Items, opt => opt.MapFrom(src => src));
            CreateMap<PagedList<AppLogResponse>, PagingResponse<AppLogResponse>>().ForMember(
                                dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
