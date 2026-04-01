using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Questionservice.Data;
using Questionservice.DTOs;
using Questionservice.Models;
using Questionservice.Services;
using System.Security.Claims;
using Wolverine;

namespace Questionservice.Controllers
{
    [ApiController]//自动验证使用的每个端点
    [Route("[controller]")]
    public class QuestionsController(QuestionDbContext db,IMessageBus bus,TagService tagService) : ControllerBase
    {
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto dto)
        {
            if (!await tagService.AreTagsValidAsync(dto.Tags))
            {
                return BadRequest("包含无效标签");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue("name");

            if (userId is null || name is null) {
                return BadRequest("无法获取用户详细信息");
            }

            var question = new Question
            {
                Title = dto.Title,
                Content = dto.Content,
                AskerId = userId,
                AskerDisplayName = name,
                TagSlugs = dto.Tags
            };

            db.Questions.Add(question);
            await db.SaveChangesAsync();

            await bus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content,question.CreateAt,question.TagSlugs));

            return Created($"/questions/{question.Id}", question); //返回201状态码，并在响应头中包含新创建资源的URL
        }

        /// <summary>
        /// 按标签返回问题
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<Question>>> GetQuestions(string? tag) 
        {
            var query = db.Questions.AsQueryable();

            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(x => x.TagSlugs.Contains(tag));
            }

            return await query.OrderByDescending(x => x.CreateAt).ToListAsync();
        }

        //按ID返回问题
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestion(string id) 
        {
            var question = await db.Questions.FindAsync(id);

            if (question is null) return NotFound();

            await db.Questions.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.viewCount,
                x => x.viewCount + 1));

            return question;
        }

        ///问题发布者修改自己发布的问题
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto dto)
        {
            var question = await db.Questions.FindAsync(id);
            if (question is null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != question.AskerId) return Forbid();

            if (!await tagService.AreTagsValidAsync(dto.Tags))
            {
                return BadRequest("包含无效标签");
            }

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.TagSlugs = dto.Tags;
            question.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await bus.PublishAsync(new QuestionUpdated(question.Id, question.Title, question.Content, question.TagSlugs.ToArray()));

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteQuestion(string id)
        {
            var question = await db.Questions.FindAsync(id);
            if (question is null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != question.AskerId) return Forbid();

            db.Questions.Remove(question);
            await db.SaveChangesAsync();

            await bus.PublishAsync(new QuestionDeleted(question.Id));

            return NoContent();

        }
    }
}
