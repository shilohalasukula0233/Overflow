using SearchService.Models;
using Typesense;

namespace SearchService.MessageHandlers
{
    public class QuestionDeletedHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(Contracts.QuestionDeleted message)
        {
            await client.DeleteDocument<SearchQuestion>("questions", message.QuestionId);
        }

    }
}
