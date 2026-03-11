using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Questionservice.Data;
using Questionservice.Models;

namespace Questionservice.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TagsController(QuestionDbContext db) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Tag>>> GetTags()
        {
            return await db.Tags.OrderBy(x => x.Name).ToListAsync();
        }
    }
}
