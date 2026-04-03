using Contracts;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;

namespace SearchService.MessageHandlers
{
    public class QuestionCreatedHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(QuestionCreated message)
        {
            var created = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

            var doc = new SearchQuestion
            {
                Id = message.QuestionId,
                Title = message.Title,
                Content = StripHtml(message.Content),
                createdAt = created,
                Tags = message.Tags.ToArray(),
            };
            await client.CreateDocument("questions", doc);

            Console.WriteLine($"id为{message.QuestionId}的问题被创建了");
        }
        private static string StripHtml(string content)
        {
            return Regex.Replace(content,"<.*?>",string.Empty);

        }
    } 
}
