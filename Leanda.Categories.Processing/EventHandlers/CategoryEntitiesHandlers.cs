using Elasticsearch.Net;
using MassTransit;
using Nest;
using Serilog;
using System;
using System.Threading.Tasks;
using Leanda.Categories.Domain.Commands;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Dynamic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Leanda.Categories.Processing.EventHandlers
{
    public class CategoryEntitiesHandlers : IConsumer<AddEntityCategories>, IConsumer<DeleteEntityCategories>
    {
        IElasticClient _elasticClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<dynamic> _nodesCollection;

        public CategoryEntitiesHandlers(IElasticClient elasticClient, IMongoDatabase database)
        {
            _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));

            _database = database ?? throw new ArgumentNullException(nameof(database));
            _nodesCollection = _database.GetCollection<dynamic>("Nodes")
                ?? throw new NullReferenceException("Cannot get the Nodes collection");
        }

        public async Task Consume(ConsumeContext<AddEntityCategories> context)
        {
            try
            {
                var hits = _elasticClient.Search<dynamic>(s => s
                    .Index("categories")
                    .Type("category")
                    .Query(q => q.QueryString(qs => qs.Query(context.Message.Id.ToString())))).Hits;
                if (hits.Any())
                {
                    JObject hitObject = JsonConvert.DeserializeObject<JObject>(hits.First().Source.ToString());
                    IEnumerable<string> categoriesIds = hitObject.Value<JArray>("CategoriesIds").Select(x => x.ToString());
                    categoriesIds = categoriesIds.Union(context.Message.CategoriesIds.Select(x => x.ToString()));
                    var patchDocument = new { CategoriesIds = categoriesIds };
                    await _elasticClient.UpdateAsync<dynamic>(hits.First().Id,
                         i => i.Doc(patchDocument).Index("categories").Type("category"));
                }
                else
                {
                    var node = await _nodesCollection.Find(new BsonDocument("_id", context.Message.Id)).FirstOrDefaultAsync()
                        ?? throw new NullReferenceException("Cannot find the Node by id: " + context.Message.Id);

                    var insertDocument = new { CategoriesIds = context.Message.CategoriesIds.Distinct(), Node = node };
                    var status = await _elasticClient.IndexAsync<dynamic>(insertDocument,
                        i => i.Index("categories").Type("category"));
                }

                Log.Information($"Document index created. Categories are: {context.Message.CategoriesIds.ToJson()}");
            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"Document index error. Categories are: {context.Message.CategoriesIds.ToJson()}");
            }
        }

        public async Task Consume(ConsumeContext<DeleteEntityCategories> context)
        {
            try
            {
                var node = await _nodesCollection.Find(new BsonDocument("_id", context.Message.Id)).FirstOrDefaultAsync()
                    ?? throw new NullReferenceException("Cannot find the Node by id: " + context.Message.Id);

                var result = _elasticClient.Search<dynamic>(s => s
                    .Index("categories")
                    .Type("category")
                    .Query(q => q.QueryString(qs => qs.Query(context.Message.Id.ToString()))));

                foreach (var hit in result.Hits)
                {
                    JObject hitObject = JsonConvert.DeserializeObject<JObject>(hit.Source.ToString());
                    IEnumerable<string> categoriesIds = hitObject.Value<JArray>("CategoriesIds").Select(x => x.ToString());
                    categoriesIds = categoriesIds.Where(x => !context.Message.CategoriesIds.Select(z => z.ToString()).Contains(x));

                    if (categoriesIds.Any())
                    {
                        var indexDocument = new { CategoriesIds = categoriesIds.Distinct() };
                        await _elasticClient.UpdateAsync<dynamic>(hit.Id,
                             i => i.Doc(indexDocument).Index("categories").Type("category"));
                    }
                    else
                    {
                        await _elasticClient.DeleteAsync(new DeleteRequest("categories", "category", hit.Id));
                    }
                }


                //context.Message.CategoriesIds.ForEach(async categoryId =>
                //{
                //    var indexDocument = new { CategoryId = categoryId, Node = node };
                //    var status = await _elasticClient.DeleteAsync<dynamic>(indexDocument,
                //        i => i.Index("categories").Type("category"));
                //});
                Log.Information($"Document index created for categories: {context.Message.CategoriesIds.ToJson()}");
            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"Document index error for categories: {context.Message.CategoriesIds.ToJson()}");
            }
        }
    }
}
