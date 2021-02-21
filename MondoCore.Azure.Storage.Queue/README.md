# MondoCore.Azure.Storage.Queue
  Sending messages through Azure Storage Queues
 
<br>


#### AzureStorageQueue

```
using MondoCore.Azure.Storage.Queue;

public static class Example
{
    public static async Task DoWork(string connectionString, string queueName)
    {
        // Create an instance of AzureStorage with a connection string and the queue name.
        IMessageQueue queue = new AzureStorageQueue(connectionString, queueName);

        string message = "Bob's your uncle";

        await store.Send(message);
    }

    // You can optionally send a message at some point in the future
    public static async Task DoWorkLater(string connectionString, string queueName)
    {
        // Create an instance of AzureStorage with a connection string and the queue name.
        IMessageQueue queue = new AzureStorageQueue(connectionString, queueName);

        string message = "Bob's your uncle";

        // Send 5 minutes from now
        await store.Send(message, DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
```

```
License
----

MIT
