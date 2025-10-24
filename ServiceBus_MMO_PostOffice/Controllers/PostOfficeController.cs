using Microsoft.AspNetCore.Mvc;
using ServiceBus_MMO_PostOffice.Data;


namespace ServiceBus_MMO_PostOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostOfficeController(ApplicationDbContext _context) : ControllerBase
    {

    }
}
