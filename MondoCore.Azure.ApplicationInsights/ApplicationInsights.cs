/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  	                                            
 *                                                                          
 *      Namespace: MondoCore.ApplicationInsights	                                        
 *           File: ApplicationInsights.cs                                       
 *      Class(es): ApplicationInsights                                          
 *        Purpose: Implementation of ILog interface for Azure Application Insights                            
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 29 Nov 2015                                            
 *                                                                          
 *   Copyright (c) 2015-2020 - Jim Lightfoot, All rights reserved           
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

using MondoCore.Common;
using MondoCore.Log;

namespace MondoCore.ApplicationInsights
{
    /*************************************************************************/
    /*************************************************************************/
    public class ApplicationInsights : ILog
    {
        private readonly TelemetryClient _client;
        private readonly bool _childrenAsJson;

        /*************************************************************************/
        public ApplicationInsights(TelemetryConfiguration telemetryConfiguration, bool childrenAsJson = true)
        {
            _client = new TelemetryClient(telemetryConfiguration);
            _childrenAsJson = childrenAsJson;
        }

        /*************************************************************************/
        public async Task WriteTelemetry(Telemetry telemetry)
        {
            await Task.Yield();
            
            switch(telemetry.Type)
            {
                case Telemetry.TelemetryType.Error:
                { 
                    var tel = new ExceptionTelemetry(telemetry.Exception);
                    
                    tel.MergeProperties(telemetry, _childrenAsJson);
                    tel.Message = telemetry.Exception.Message;
                    tel.SeverityLevel = (SeverityLevel)((int)telemetry.Severity);

                    SetAttributes(telemetry, tel, tel);

                    _client.TrackException(tel);

                    break;
                }

                case Telemetry.TelemetryType.Event:
                { 
                    var tel = new EventTelemetry(telemetry.Message);

                    tel.MergeProperties(telemetry, _childrenAsJson);

                    SetAttributes(telemetry, tel, tel);

                    _client.TrackEvent(tel);

                    break;
                }

                case Telemetry.TelemetryType.Metric:
                { 
                    var tel = new MetricTelemetry(telemetry.Message, telemetry.Value);

                    tel.MergeProperties(telemetry, _childrenAsJson);

                    SetAttributes(telemetry, tel, tel);

                    _client.TrackMetric(tel);

                    break;
                }

                case Telemetry.TelemetryType.Trace:
                { 
                    var tel = new TraceTelemetry(telemetry.Message, (SeverityLevel)((int)telemetry.Severity));

                    tel.MergeProperties(telemetry, _childrenAsJson);

                    SetAttributes(telemetry, tel, tel);

                    _client.TrackTrace(tel);

                    break;
                }

                case Telemetry.TelemetryType.Request:
                { 
                    var tel = new RequestTelemetry(telemetry.Message, 
                                                   telemetry.Request.StartTime, 
                                                   telemetry.Request.Duration, 
                                                   telemetry.Request.ResponseCode,
                                                   telemetry.Request.Success);

                    tel.MergeProperties(telemetry, _childrenAsJson);

                    SetAttributes(telemetry, tel, tel);

                    _client.TrackRequest(tel);

                    break;
                }
            }

            _client.Flush();

            return;
        }

        /*************************************************************************/
        public IDisposable StartOperation(string operationName)
        {
            return new Operation(_client, operationName);
        }

        /*************************************************************************/
        public IRequestLog NewRequest(string operationName = null, string correlationId = null)
        {
            throw new NotSupportedException();
        }

        #region Private

        /*************************************************************************/
        private static void SetAttributes(Telemetry telemetry, ITelemetry aiTelemetry, ISupportProperties properties)
        {
            if(!string.IsNullOrWhiteSpace(telemetry.CorrelationId))
            { 
                aiTelemetry.Context.Operation.Id = telemetry.CorrelationId;
                aiTelemetry.Context.Operation.Name = telemetry.OperationName;
            }
        }

        /*************************************************************************/
        /*************************************************************************/
        private class Operation : IDisposable
        {
            private readonly IOperationHolder<RequestTelemetry> _op;
            private readonly TelemetryClient _client;

            /*************************************************************************/
            internal Operation(TelemetryClient client, string operationName)
            {
                _client = client;
                _op = client.StartOperation<RequestTelemetry>(operationName);
            }

            /*************************************************************************/
            public void Dispose()
            {
                _client.StopOperation(_op);
                _op.Dispose();
            }
        }

        #endregion
    }
}
