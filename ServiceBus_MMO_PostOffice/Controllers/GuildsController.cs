using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.Models;

namespace ServiceBus_MMO_PostOffice.Controllers
{

    //CLASS CRUD AUTO GENERATED IN VISUAL STUDIO
    //CRUDS ARE NOT THE FOCUS OF THIS PROJECT!!!
    //I WILL LEAVE IT AS IS WITHOUT ANY MODIFICATIONS
    //THIS SUITS THE PURPOSE OF THE PROJECT JUST FINE

    [Route("api/[controller]")]
    [ApiController]
    public class GuildsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GuildsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Guild>>> GetGuild()
        {
            return await _context.Guild.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Guild>> GetGuild(int id)
        {
            var guild = await _context.Guild.FindAsync(id);

            if (guild == null)
            {
                return NotFound();
            }

            return guild;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuild(int id, Guild guild)
        {
            if (id != guild.Id)
            {
                return BadRequest();
            }

            _context.Entry(guild).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GuildExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Guild>> PostGuild(Guild guild)
        {
            _context.Guild.Add(guild);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGuild", new { id = guild.Id }, guild);
        }

        // DELETE: api/Guilds/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuild(int id)
        {
            var guild = await _context.Guild.FindAsync(id);
            if (guild == null)
            {
                return NotFound();
            }

            _context.Guild.Remove(guild);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GuildExists(int id)
        {
            return _context.Guild.Any(e => e.Id == id);
        }
    }
}
