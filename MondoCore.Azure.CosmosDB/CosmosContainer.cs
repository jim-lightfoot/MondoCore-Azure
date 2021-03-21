using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

using MondoCore.Common;
using MondoCore.Data;

namespace MondoCore.Azure.CosmosDB
{
    internal abstract class CosmosContainer<TID> 
    {
        private readonly IIdentifierStrategy<TID> _idStrategy;

        internal CosmosContainer(Container container, IIdentifierStrategy<TID> strategy)
        {
            this.Container = container;
            _idStrategy = strategy;
        }

        internal protected Container Container { get; }

        internal protected (string Id, PartitionKey PartitionKey) SplitId(TID id)
        {
            var sid = "";
            var partitionKey = PartitionKey.Null;

            if(id is IPartitionedId partitionedId)
            {
                sid = partitionedId.Id;
    
                if(!string.IsNullOrWhiteSpace(partitionedId.PartitionKey))
                { 
                    partitionKey = new PartitionKey(partitionedId.PartitionKey);

                    return (sid, partitionKey);
                }
            }

            if(_idStrategy != null)
            { 
                var idResult = _idStrategy.GetId(id);
                
                if(string.IsNullOrWhiteSpace(sid))
                    sid = idResult.Id;

                if(partitionKey == PartitionKey.Null)
                    partitionKey = new PartitionKey(idResult.PartitionKey);
            }

            return (sid, partitionKey);
        }
     
        protected async Task<TValue> InternalGet<TValue>(string id, PartitionKey partitionKey)
        {
            try
            { 
                var result = await this.Container.ReadItemAsync<TValue>(id, partitionKey);
            
                if(result == null)
                    throw new NotFoundException();

                return result;
            }
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        protected async IAsyncEnumerable<TValue> InternalGet<TValue>(Expression<Func<TValue, bool>> query)
        {
            using(var feedIterator = this.Container.GetItemLinqQueryable<TValue>()
                                                   .Where(query)
                                                   .ToFeedIterator())
            {
                while(feedIterator.HasMoreResults)
                {
                    foreach(var item in await feedIterator.ReadNextAsync())
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}
