using System;

using Microsoft.Azure.Cosmos;

using MondoCore.Data;

namespace MondoCore.Azure.CosmosDB
{
    /// <summary>
    /// Provides methods to access a CosmosDB
    /// </summary>
    public class CosmosDB : IDatabase
    {
        protected readonly Database _db;

        public CosmosDB(string dbName, string connectionString)
        {
            var client = new CosmosClient(connectionString);

            _db = client.GetDatabase(dbName);
        }

        /// <summary>
        /// Create a repository reader for the CosmosDB container (SQL API) 
        /// </summary>
        /// <typeparam name="TID">Type of the identifier</typeparam>
        /// <typeparam name="TValue">Type of the value stored in the collection</typeparam>
        /// <param name="repoName">Name of CosmosDB container</param>
        /// <returns>A reader to make read operations</returns>
        public IReadRepository<TID, TValue> GetRepositoryReader<TID, TValue>(string repoName, IIdentifierStrategy<TID> strategy) where TValue : IIdentifiable<TID>
        {
            var cosmosContainer = _db.GetContainer(repoName);

            return new CosmosContainerReader<TID, TValue>(cosmosContainer, strategy);
        }

       /// <summary>
        /// Create a repository wtier for the CosmosDB container (SQL API) 
        /// </summary>
        /// <typeparam name="TID">Type of the identifier</typeparam>
        /// <typeparam name="TValue">Type of the value stored in the collection</typeparam>
        /// <param name="repoName">Name of v</param>
        /// <returns>A writer to make writer operations</returns>
        public IWriteRepository<TID, TValue> GetRepositoryWriter<TID, TValue>(string repoName, IIdentifierStrategy<TID> strategy) where TValue : IIdentifiable<TID>
        {
            var cosmosContainer = _db.GetContainer(repoName);

            return new CosmosContainerWriter<TID, TValue>(cosmosContainer, strategy);
        }
    }

}
