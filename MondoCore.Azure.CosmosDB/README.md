# MondoCore.Azure.CosmosDB
  Access to Azure Cosmos Database (using SQL API)
 
<br>


#### CosmosDB

```
using MondoCore.Azure.CosmosDB;

public static class Example
{
    public static async Task DoWork(string connectionString, string containerName)
    {
        // Create an instance of CosmosDB with a connection string 
        //   The container name can also optionally have a folder path
        var db = new CosmosDB("myDB", connectionString);

        // Get a reader for the container with a specific partition key
        var reader = _db.GetRepositoryReader<string, Automobile>("cars", "Chevy");

        // Retrieve object
        var car = await reader.Get("1234");

        Console.Write(car)
    }
}
```

```
License
----

MIT
