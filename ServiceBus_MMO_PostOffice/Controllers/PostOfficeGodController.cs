using Microsoft.AspNetCore.Mvc;
using ServiceBus_MMO_PostOffice.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ServiceBus_MMO_PostOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostOfficeGodController(ApplicationDbContext _context) : ControllerBase
    {

    }
}
