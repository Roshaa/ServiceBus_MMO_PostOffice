using AutoMapper;
using ServiceBus_MMO_PostOffice.DTO_s;
using ServiceBus_MMO_PostOffice.Messages.MessageTypes;
using ServiceBus_MMO_PostOffice.Models;
using SharedClasses.Messaging;

namespace ServiceBus_MMO_PostOffice.Mappers
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            //Player
            CreateMap<CreatePlayerDTO, Player>();
            CreateMap<Player, PlayerCreated>();
            CreateMap<Player, PlayerDTO>()
                .ForMember(d => d.Guild, o => o.MapFrom(s => s.Guild));
            CreateMap<PlayerDTO, Player>();

            //Guild
            CreateMap<Guild, GuildDTO>();
            CreateMap<GuildDTO, Guild>();
            CreateMap<CreateGuildDTO, Guild>();

            //Raid
            CreateMap<RaidParticipant, RaidParticipantsDTO>()
                .ForMember(d => d.NickName, o => o.MapFrom(s => s.Player.NickName));
            CreateMap<Raid, RaidDTO>()
                .ForMember(d => d.RaidParticipants, o => o.MapFrom(s => s.RaidParticipant));
            CreateMap<CreateRaidDTO, Raid>();
            CreateMap<Raid, RaidEvent>();
        }
    }
}
