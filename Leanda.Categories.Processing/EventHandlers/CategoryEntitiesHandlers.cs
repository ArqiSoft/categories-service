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

namespace Leanda.Categories.Processing.EventHandlers
{
    public class CategoryEntitiesHandlers : IConsumer<AddCategoriesToEntity>
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

        public async Task Consume(ConsumeContext<AddCategoriesToEntity> context)
        {
            try
            {
                var node = await _nodesCollection.Find(new BsonDocument("_id", context.Message.Id)).FirstOrDefaultAsync()
                    ?? throw new NullReferenceException("Cannot find the Node by id: " + context.Message.Id);
                context.Message.CategoriesIds.ForEach(async categoryId =>
                {
                    var indexDocument = new { CategoryId = categoryId, Node = node };
                    var status = await _elasticClient.IndexAsync<dynamic>(indexDocument,
                        i => i.Index("categories").Type("category"));
                });
                Log.Information($"Document index created. Categories are: {context.Message.CategoriesIds.ToJson()}");
            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"Document index error. Categories are: {context.Message.CategoriesIds.ToJson()}");
            }
        }

        public async Task Consume(ConsumeContext<DeleteCategoriesFromEntity> context)
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
                    var categoriesIds = JsonConvert.DeserializeObject<List<string>>(hit.Source.CategoriesIds);
                    var indexDocument = new { CategoriesIds = categoriesIds};
                    _elasticClient.UpdateAsync<dynamic>(indexDocument,
                        i => i.Index("categories").Type("category"));
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
