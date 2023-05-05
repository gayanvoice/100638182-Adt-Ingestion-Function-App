// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.Net.Http;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;
using System.Threading.Tasks;

namespace UodAdtInjectionFunctionApp
{
    public static class Function
    {
        private static readonly string adtInstanceUrl = "https://100638182AzureDigitalTwins.api.uks.digitaltwins.azure.net";

        [FunctionName("AdtIngestion")]
        public static async Task RunAsync([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            //https://learn.microsoft.com/en-us/azure/digital-twins/how-to-ingest-iot-hub-data
            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");

            try
            {
                ManagedIdentityCredential cred = new ManagedIdentityCredential();
                var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), cred);

                log.LogInformation($"ADT service client connection created.");

                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    log.LogInformation(eventGridEvent.Data.ToString());

                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    string deviceId = "XiaomiDevice1";
                    var illuminance = deviceMessage["body"]["Illuminance"];

                    log.LogInformation($"Device:{deviceId} Illuminance is:{illuminance}");
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendReplace("/Illuminance", illuminance.Value<float>());

                    log.LogInformation($"updateTwinData: {updateTwinData.ToString()}");

                    await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex.Message}");
            }
        }
    }
}
