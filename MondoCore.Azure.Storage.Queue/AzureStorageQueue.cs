/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  							                    
 *                                                                          
 *        Namespace: MondoCore.Azure.Storage.Queue			            
 *             File: AzureStorageQueue.cs			 		    		    
 *        Class(es): AzureStorageQueue				           		        
 *          Purpose: An Azure Storage Queue implementation of IMessageQueue                       
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 15 Aug 2020                                             
 *                                                                          
 *   Copyright (c) 2020 - Jim Lightfoot, All rights reserved                
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.Linq;
using System.Threading.Tasks;

using Azure.Storage.Queues; 
using Azure.Storage.Queues.Models; 

using MondoCore.Common;

namespace MondoCore.Azure.Storage.Queue
{
    /****************************************************************************/
    /****************************************************************************/
    /// <summary>
    /// An Azure Storage Queue implementation of IMessageQueue
    /// </summary>
    public class AzureStorageQueue : IMessageQueue
    {
        private readonly QueueClient _queueClient;

        /****************************************************************************/
        public AzureStorageQueue(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);

            _queueClient.CreateIfNotExists();        
        }

        /****************************************************************************/
        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sendOn"></param>
        public Task Send(string message, DateTimeOffset? sendOn = null)
        {
            var visibility = sendOn == null ? null : DateTimeOffset.UtcNow - sendOn;

            return _queueClient.SendMessageAsync(message, visibility);
        }

        /****************************************************************************/
        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sendOn"></param>
        public async Task<IMessage> Retrieve()
        {
            var msgs = await _queueClient.ReceiveMessagesAsync(1, new TimeSpan(0, 5, 0)).ConfigureAwait(false);

            return new AzureStorageQueueMessage(msgs.Value[0]);
        }

        /****************************************************************************/
        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sendOn"></param>
        public Task Delete(IMessage msg)
        {
            var amsg = msg as AzureStorageQueueMessage;

            if(amsg == null)
                throw new ArgumentException("Message is not a valid type for this queue");

            return _queueClient.DeleteMessageAsync(amsg.Message.MessageId, amsg.Message.PopReceipt);
        }
    }

    /****************************************************************************/
    /****************************************************************************/
    /// <summary>
    /// An Azure Storage Queue implementation of IMessage
    /// </summary>
    internal class AzureStorageQueueMessage : IMessage
    {
        private readonly QueueMessage _message;

        /****************************************************************************/
        internal AzureStorageQueueMessage(QueueMessage message)
        {
            _message = message;
        }

        internal QueueMessage Message => _message;
        public string         Value   => _message.MessageText;
    }

    /****************************************************************************/
    /****************************************************************************/
    public class AzureStorageQueueFactory : IMessageQueueFactory
    {
        private readonly string _connectionString;

        /****************************************************************************/
        public AzureStorageQueueFactory(string connectionString)
        {
            _connectionString = connectionString;        
        }

        /****************************************************************************/
        /// <summary>
        /// Create a queue with the given name
        /// </summary>
        /// <param name="queueName">The name of the queue to create</param>
        /// <returns>An instance of AzureStorageQueue</returns>
        public IMessageQueue CreateQueue(string queueName)
        {
            return new AzureStorageQueue(_connectionString, queueName);
        }
    }
}
