using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.DTO_s;
using ServiceBus_MMO_PostOffice.Models;
using ServiceBus_MMO_PostOffice.Services;
using SharedClasses.Contracts;
using SharedClasses.Messaging;

namespace ServiceBus_MMO_PostOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaidsController(ApplicationDbContext _context, IMapper _mapper, PostOfficeServiceBusPublisher _publisher) : ControllerBase
    {
        private readonly AutoMapper.IConfigurationProvider _mapConfig = _mapper.ConfigurationProvider;

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetRaid(int id)
        {
            RaidDTO raid = await _context.Raid
                .Where(r => r.Id == id)
                .ProjectTo<RaidDTO>(_mapConfig)
                .FirstOrDefaultAsync();

            return raid is null ? NotFound() : Ok(raid);
        }

        [HttpGet("by_guild/{guildId:int}")]
        public async Task<ActionResult> GetRaidsByGuild(int guildId)
        {
            RaidDTO[] raids = await _context.Raid
                .Where(r => r.GuildId == guildId)
                .ProjectTo<RaidDTO>(_mapConfig)
                .ToArrayAsync();

            return Ok(raids);
        }

        [HttpPost]
        public async Task<ActionResult> CreateRaid([FromBody] CreateRaidDTO dto)
        {
            Raid raid = _mapper.Map<Raid>(dto);

            _context.Raid.Add(raid);
            await _context.SaveChangesAsync();

            int[] guildMembersId = GetGuildMembersById(raid.GuildId);

            List<ServiceBusMessage> messages = new List<ServiceBusMessage>();

            RaidEvent invite = _mapper.Map<RaidEvent>(raid);
            invite.Message = $"You are invited to a raid for guild {raid.Guild.Name}";

            TimeSpan ttl = raid.StartTime.AddMinutes(30) - DateTime.UtcNow;

            foreach (var memberId in guildMembersId)
            {
                RaidParticipant participant = new RaidParticipant
                {
                    PlayerId = memberId,
                    RaidId = raid.Id,
                };

                _context.RaidParticipant.Add(participant);

                messages.Add(_publisher.CreateMessage<RaidEvent>(invite, RaidEventsSubscription.RaidInviteSubject, ttl, participant.PlayerId.ToString()));
            }

            await _context.SaveChangesAsync();
            await _publisher.PublishBatchAsync(messages);

            var raidDto = await _context.Raid
            .Where(r => r.Id == raid.Id)
            .ProjectTo<RaidDTO>(_mapConfig)
            .SingleAsync();

            return CreatedAtAction(nameof(GetRaid), new { id = raid.Id }, raidDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ChangeRaidInviteStatus(int id)
        {
            RaidParticipant? participant = await _context.RaidParticipant.FirstOrDefaultAsync(r => r.Id == id);

            if (participant == null) return NotFound();

            participant.InviteAccepted = !participant.InviteAccepted;
            await _context.SaveChangesAsync();

            return Ok(participant.InviteAccepted);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRaid(int id)
        {
            var raid = await _context.Raid.FindAsync(id);
            if (raid == null) return NotFound();
            int guildId = raid.GuildId;

            _context.Raid.Remove(raid);
            await _context.SaveChangesAsync();

            int[] guildMembersId = GetGuildMembersById(guildId);

            List<ServiceBusMessage> messages = new List<ServiceBusMessage>();

            RaidEvent invite = _mapper.Map<RaidEvent>(raid);
            invite.Message = $"The raid starting at {raid.StartTime:u} has been cancelled.";

            foreach (var memberId in guildMembersId)
                messages.Add(_publisher.CreateMessage<RaidEvent>(invite, RaidEventsSubscription.RaidCancelledSubject, null, memberId.ToString()));

            await _publisher.PublishBatchAsync(messages);

            return NoContent();
        }


        //in a real project i would never put this here
        private int[] GetGuildMembersById(int guildId)
        {
            return _context.Player
                .AsNoTracking()
                .Where(g => g.GuildId == guildId)
                .Select(m => m.Id)
                .ToArray();
        }

    }
}
