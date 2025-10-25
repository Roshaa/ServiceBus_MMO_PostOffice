using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.DTO_s;
using ServiceBus_MMO_PostOffice.Models;

namespace ServiceBus_MMO_PostOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaidsController(ApplicationDbContext _context, IMapper _mapper) : ControllerBase
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
        public async Task<ActionResult> CreateRaid(CreateRaidDTO dto)
        {
            Raid raid = _mapper.Map<Raid>(dto);

            _context.Raid.Add(raid);
            await _context.SaveChangesAsync();

            int[] guildMembersId = await _context.Player
                .AsNoTracking()
                .Where(g => g.GuildId == dto.GuildId)
                .Select(m => m.Id)
                .ToArrayAsync();

            foreach (var memberId in guildMembersId)
            {
                RaidParticipant participant = new RaidParticipant
                {
                    PlayerId = memberId,
                    RaidId = raid.Id,
                };

                _context.RaidParticipant.Add(participant);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRaid", new { id = raid.Id }, raid);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRaid(int id)
        {
            var raid = await _context.Raid.FindAsync(id);
            if (raid == null) return NotFound();

            _context.Raid.Remove(raid);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
