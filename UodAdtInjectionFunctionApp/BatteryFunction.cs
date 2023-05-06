// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AdtInjectionCameraFunctionApp
{
    public static class BatteryFunction
    {
        private static readonly string adtConnectionString = Environment.GetEnvironmentVariable("ADT_CONNECTION_STRING");
        [FunctionName("BatteryAdtIngestion")]
        public static async Task RunAsync([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            //https://learn.microsoft.com/en-us/azure/digital-twins/how-to-ingest-iot-hub-data
            if (adtConnectionString == null) log.LogError("Application setting ADT_CONNECTION_STRING not set");

            try
            {
                ManagedIdentityCredential cred = new ManagedIdentityCredential();
                var client = new DigitalTwinsClient(new Uri(adtConnectionString), cred);

                log.LogInformation($"ADT service client connection created.");

                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    log.LogInformation(eventGridEvent.Data.ToString());

                    JObject deviceMessage = (JObject) JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    string deviceId = (string) deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    double temperature = (double) deviceMessage["body"]["Temperature"];
                    int voltage = (int) deviceMessage["body"]["Voltage"];
                    bool isCharging = (bool) deviceMessage["body"]["IsCharging"];
                    string power = (string) deviceMessage["body"]["Power"];

                    log.LogInformation($"Device:{deviceId} Temperature {temperature} Voltage {voltage} IsCharging {isCharging} Power {power}");
                    
                    Azure.JsonPatchDocument jsonPatchDocument = new Azure.JsonPatchDocument();
                    jsonPatchDocument.AppendReplace("/Temperature", temperature);
                    jsonPatchDocument.AppendReplace("/Voltage", voltage);
                    jsonPatchDocument.AppendReplace("/IsCharging", isCharging);
                    jsonPatchDocument.AppendReplace("/Power", power);

                    log.LogInformation($"JsonPatchDocument: {jsonPatchDocument}");

                    await client.UpdateDigitalTwinAsync(deviceId, jsonPatchDocument);
                }
            }
            catch (Exception ex)
            {
               // log.LogError($"Error in BatteryAdtIngestion function: {ex.Message}");
            }
        }
    }
}
