using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Questionservice.Data;
using Questionservice.Models;

namespace Questionservice.Services
{
    public class TagService(IMemoryCache cache, QuestionDbContext db)
    {
        private const string CacheKey = "tags";//缓存键

        private async Task<List<Tag>> GetTags()
        {
            return await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120);//设置缓存过期时间

                var tags = await db.Tags.AsNoTracking().ToListAsync();//从数据库获取标签列表

                return tags;
            }) ?? [];
        }

        public async Task<bool> AreTagsValidAsync(List<string> slugs)
        {
            var tags = await GetTags();
            var tagSet = tags.Select(x => x.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);//创建一个哈希集合，包含所有标签的Slug，使用不区分大小写的比较器
            return slugs.All(x => tagSet.Contains(x));//检查每个输入的Slug是否都存在于标签集合中
        }
    }
}
