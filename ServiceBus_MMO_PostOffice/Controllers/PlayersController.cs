using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.DTO_s;
using ServiceBus_MMO_PostOffice.Messages.MessageTypes;
using ServiceBus_MMO_PostOffice.Models;
using ServiceBus_MMO_PostOffice.Services;
using SharedClasses.Contracts;

namespace ServiceBus_MMO_PostOffice.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController(ApplicationDbContext db, IMapper _mapper, PostOfficeServiceBusPublisher _publisher) : ControllerBase
    {
        private readonly AutoMapper.IConfigurationProvider _mapConfig = _mapper.ConfigurationProvider;

        [HttpGet]
        public async Task<ActionResult> GetLatestPlayers([FromQuery] int page = 1, CancellationToken ct = default)
        {
            int PlayersPerPage = 30;
            int skip = (page - 1) * PlayersPerPage;

            PlayerDTO[] players = await db.Player
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(PlayersPerPage)
                .ProjectTo<PlayerDTO>(_mapConfig)
                .ToArrayAsync(ct);

            return Ok(players);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetPlayer([FromRoute] int id, CancellationToken ct = default)
        {
            PlayerDTO player = await db.Player
                .AsNoTracking()
                .ProjectTo<PlayerDTO>(_mapConfig)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return player is null ? NotFound() : Ok(player);
        }

        [HttpPost]
        public async Task<ActionResult<Player>> PostPlayer([FromBody] CreatePlayerDTO createPlayerDTO, CancellationToken ct = default)
        {
            Player player = _mapper.Map<Player>(createPlayerDTO);

            await db.Player.AddAsync(player);
            await db.SaveChangesAsync(ct);

            PlayerCreated playerCreated = _mapper.Map<PlayerCreated>(player);

            ServiceBusMessage sbMessage = _publisher.CreateMessage(playerCreated, PlayerCreatedSubscription.Subject);

            await _publisher.PublishMessageAsync(sbMessage);

            return CreatedAtAction("GetPlayer", new { id = player.Id }, player);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer([FromRoute] int id, CancellationToken ct = default)
        {
            Player player = await db.Player.FindAsync(id);
            if (player == null) return NotFound();

            db.Player.Remove(player);
            await db.SaveChangesAsync(ct);

            return NoContent();
        }

    }
}
