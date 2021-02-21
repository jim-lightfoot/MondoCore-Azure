# MondoCore.Azure.Storage
  Blob storage in Azure Storage
 
<br>


#### AzureStorage

```
using MondoCore.Azure.Storage;

public static class Example
{
    public static async Task DoWork(string connectionString, string containerName)
    {
        // Create an instance of AzureStorage with a connection string and the container name.
        //   The container name can also optionally have a folder path
        IBlobStore store = new AzureStorage(connectionString, containerName);

        string content = "Bob's your uncle";

        await store.Put("bob", content);

        var result = store.Get("bob");

        Console.Write(result == content ? "result == content" : "result != content")
    }
}
```

```
License
----

MIT
