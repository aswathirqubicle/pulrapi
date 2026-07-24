using System.Linq;
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models.MediaFiles;
using Core.Domain.Entities;

namespace Core.Application.Mappings
{
    public class MediaFileProfile : AutoMapper.Profile
    {
        public MediaFileProfile()
        {
            CreateMap<MediaFile, MediaFileDetailsResponse>()
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.MediaFileType.ToString()))
                .ForMember(dest => dest.IsMuted, opt => opt.MapFrom(src => src.IsMuted));

            CreateMap<ProductMediaFile, MediaFileDetailsResponse>()
                .ForMember(dest => dest.Uid, opt => opt.MapFrom(src => src.MediaFile.Uid))
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.MediaFile.Url))
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.MediaFile.MediaFileType.ToString()))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.MediaFile.Priority))
                .ForMember(dest => dest.OriginalUrl, opt => opt.MapFrom(src => src.MediaFile.OriginalUrl))
                .ForMember(dest => dest.IsHlsProcessed, opt => opt.MapFrom(src => src.MediaFile.IsHlsProcessed))
                .ForMember(dest => dest.HlsBasePath, opt => opt.MapFrom(src => src.MediaFile.HlsBasePath))
                .ForMember(dest => dest.VideoDurationSeconds, opt => opt.MapFrom(src => src.MediaFile.VideoDurationSeconds))
                .ForMember(dest => dest.AvailableQualities, opt => opt.MapFrom(src => src.MediaFile.AvailableQualities))
                .ForMember(dest => dest.IsMuted, opt => opt.MapFrom(src => src.MediaFile.IsMuted))
                .ForMember(dest => dest.CropX, opt => opt.MapFrom(src => src.MediaFile.CropX))
                .ForMember(dest => dest.CropY, opt => opt.MapFrom(src => src.MediaFile.CropY))
                .ForMember(dest => dest.CropWidth, opt => opt.MapFrom(src => src.MediaFile.CropWidth))
                .ForMember(dest => dest.CropHeight, opt => opt.MapFrom(src => src.MediaFile.CropHeight));
        }
    }
}
