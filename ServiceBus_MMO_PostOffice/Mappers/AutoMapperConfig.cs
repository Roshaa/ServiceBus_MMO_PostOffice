using AutoMapper;
using ServiceBus_MMO_PostOffice.DTO_s;
using ServiceBus_MMO_PostOffice.Messages.MessageTypes;
using ServiceBus_MMO_PostOffice.Models;

namespace ServiceBus_MMO_PostOffice.Mappers
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<Player, PlayerDTO>()
                .ForMember(d => d.Guild, o => o.MapFrom(s => s.Guild));

            CreateMap<PlayerDTO, Player>();
            CreateMap<CreatePlayerDTO, Player>();

            CreateMap<Guild, GuildDTO>();
            CreateMap<GuildDTO, Guild>();
            CreateMap<CreateGuildDTO, Guild>();
            CreateMap<Player, PlayerCreated>();
        }
    }
}
