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
    public class GuildsController(ApplicationDbContext db, IMapper _mapper) : ControllerBase
    {
        private readonly AutoMapper.IConfigurationProvider _mapConfig = _mapper.ConfigurationProvider;

        [HttpGet]
        public async Task<ActionResult> GetLatestGuilds([FromQuery] int page = 1, CancellationToken ct = default)
        {
            int GuildsPerPage = 30;
            int skip = (page - 1) * GuildsPerPage;

            GuildDTO[] guilds = await db.Guild
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(GuildsPerPage)
                .ProjectTo<GuildDTO>(_mapConfig)
                .ToArrayAsync(ct);

            return Ok(guilds);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetGuild([FromRoute] int id, CancellationToken ct = default)
        {
            GuildDTO guild = await db.Guild
                .AsNoTracking()
                .ProjectTo<GuildDTO>(_mapConfig)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return guild is null ? NotFound() : Ok(guild);
        }

        [HttpPost]
        public async Task<ActionResult<Guild>> PostGuild([FromBody] CreateGuildDTO createGuildDTO, CancellationToken ct = default)
        {
            Guild guild = _mapper.Map<Guild>(createGuildDTO);

            await db.Guild.AddAsync(guild);
            await db.SaveChangesAsync(ct);

            return CreatedAtAction("GetGuild", new { id = guild.Id }, guild);
        }

        [HttpDelete("delete_guild_{id:int}")]
        public async Task<IActionResult> DeleteGuild([FromRoute] int id, CancellationToken ct = default)
        {
            Guild? guild = await db.Guild.FindAsync(id);
            if (guild == null) return NotFound();

            await db.Player
                .Where(p => p.GuildId == id)
                .ForEachAsync(p => p.GuildId = null, ct);

            db.Guild.Remove(guild);
            await db.SaveChangesAsync(ct);

            return NoContent();
        }

        [HttpPost("add_player_to_guild")]
        public async Task<IActionResult> AddPlayerToGuild([FromBody] GuildRelationDTO relationDTO, CancellationToken ct = default)
        {
            bool guildExists = await db.Guild.AsNoTracking().AnyAsync(g => g.Id == relationDTO.GuildId, ct);
            if (!guildExists) return NotFound($"Guild {relationDTO.GuildId} not found.");

            Player? player = await db.Player.FirstOrDefaultAsync(p => p.Id == relationDTO.PlayerId, ct);
            if (player is null) return NotFound($"Player {relationDTO.PlayerId} not found.");

            player.GuildId = relationDTO.GuildId;
            await db.SaveChangesAsync(ct);

            return Ok();
        }

        [HttpDelete("remove_player_{playerId:int}")]
        public async Task<IActionResult> RemovePlayerFromGuild([FromRoute] int playerId, CancellationToken ct = default)
        {
            Player? player = await db.Player.FirstOrDefaultAsync(p => p.Id == playerId, ct);
            if (player is null) return NotFound($"Player {playerId} not found.");

            player.GuildId = null;
            await db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}