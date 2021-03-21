using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using MondoCore.Common;
using MondoCore.Data;

namespace MondoCore.Azure.CosmosDB
{
   internal class CosmosContainerReader<TID, TValue> : CosmosContainer<TID>, IReadRepository<TID, TValue> where TValue : IIdentifiable<TID>
    {
        internal CosmosContainerReader(Container container, IIdentifierStrategy<TID> strategy) : base(container, strategy)
        {
        }

        #region IReadRepository

        public Task<TValue> Get(TID id)
        {
            var idResult = SplitId(id);

            return InternalGet<TValue>(idResult.Id, idResult.PartitionKey);
        }

        public async IAsyncEnumerable<TValue> Get(IEnumerable<TID> ids)
        {
            foreach(var id in ids)
            { 
                yield return await Get(id);
            }
        }

        public IAsyncEnumerable<TValue> Get(Expression<Func<TValue, bool>> query)
        {
            return InternalGet<TValue>(query);
        }

        #region IQueryable<>

        #region IQueryable

        public Type             ElementType => typeof(TValue);
        public Expression       Expression  => this.Container.GetItemLinqQueryable<TValue>(true).Expression;
        public IQueryProvider   Provider    => this.Container.GetItemLinqQueryable<TValue>(true).Provider;

        #endregion

        #region IEnumerable<>

        public IEnumerator<TValue> GetEnumerator() => this.Container.GetItemLinqQueryable<TValue>(true).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
          => this.Container.GetItemLinqQueryable<TValue>(true).GetEnumerator();

        #endregion

        #endregion

        #endregion
    }

}
