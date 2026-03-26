using AutoMapperProfile = AutoMapper.Profile;

namespace Dashboard.Application.Mappings
{
    public interface IMapFrom<T>
    {
      void Mapping(AutoMapperProfile profile) => profile.CreateMap(typeof(T), GetType());
    }
}
