using Typesense;

namespace SearchService.Data
{
    public static class Searchinitializer
    {
        public static async Task EnsureIndexExists(ITypesenseClient client)
        {
            const string schemaName = "questions";
            {
                try
                {
                    await client.RetrieveCollection(schemaName);
                    Console.WriteLine($"Collection '{schemaName}' 已创建");
                    return;
                }
                catch (TypesenseApiNotFoundException)
                {
                    Console.WriteLine($"Collection '{schemaName}' 未能成功被创建");
                }

                var schema = new Schema(schemaName, new List<Field>
                {
                    new("id", FieldType.String),
                    new("title", FieldType.String),
                    new("content", FieldType.String),
                    new("tags", FieldType.StringArray),
                    new("createdAt", FieldType.Int64),
                    new("hasAcceptedAnswer", FieldType.Bool),
                    new("answerCount", FieldType.Int32)
                })
                {
                    DefaultSortingField = "createdAt"
                };

                await client.CreateCollection(schema);
                Console.WriteLine($"Collection '{schemaName}' 已创建");
            }
        }
    }
}
