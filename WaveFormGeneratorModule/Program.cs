using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Net;
using AzureIotEdgeSimulatedWaveSensor;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace WaveFormGeneratorModule
{

    class Program
    {
        static object PropsLocker = new object();
        static int counter;
        static bool isEdgeModule;
        private static DesiredPropertiesData desiredPropertiesData;
        private static SimulatedWaveSensor simulatedWaveSensor;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication<Program>();

            // used to pull default values from DPD object.
            var dpv = new DesiredPropertiesData();

            app.HelpOption("-?|-h|--help",inherited: true);

            var optionStandAlone    = app.Option("-sa|--stand-alone", "Enable Stand-Alone mode to run simulator outside of Azure IoT Edge", CommandOptionType.NoValue);
            var optionSendData      = app.Option("-s|--send-data", "Enable sending of data", CommandOptionType.NoValue);    
            var optionSendInterval  = app.Option("-si|--send-interval <INTERVAL>", $"The interval to send data. Defaults to {dpv.SendInterval} seconds.", CommandOptionType.SingleOrNoValue);
            var optionFrequency     = app.Option("-f|--frequency", $"Frequency of wave measureing reading. Defaults to {dpv.Frequency} seconds ({(1/(dpv.Frequency)).ToString("F3")} hz).", CommandOptionType.SingleOrNoValue);
            var optionAmplitude     = app.Option("-a|--amplitude", $"Amplitude of the wave.", CommandOptionType.SingleOrNoValue);    
            var optionVerticalShift = app.Option("-vs|--vertical-shift", $"Positive or negative offset.", CommandOptionType.SingleOrNoValue);    
            var optionWaveType      = app.Option("-wt|--wave-type", $"The type of wave data to generate. Optional values are ({String.Join('|',Enum.GetNames(typeof(Waves)))}). Defaults to {dpv.WaveType.ToString()}", CommandOptionType.SingleOrNoValue).Accepts(v => v.Enum<Waves>(ignoreCase: true));    
            var optionIsNoisy       = app.Option("-n|--noisy", $"If flag is present, aberations in wave value will be output.", CommandOptionType.NoValue);    
            var optionDuration      = app.Option("-d|--duration", $"Length of noise generation in seconds. If 'is noisy'. Defaults to {dpv.Duration} seconds ({(1/(dpv.Duration)).ToString("F3")} hz).", CommandOptionType.SingleOrNoValue);    
            var optionStartValue    = app.Option("-v|--start", $"The start time in the wave for noise generation. Defaults to {dpv.Start}", CommandOptionType.SingleOrNoValue);    
            var optionMinNoiseBound = app.Option("-min|--min-noise-bound", $"The min aberant data value for noise. Defaults to {dpv.MinNoiseBound}", CommandOptionType.SingleOrNoValue);    
            var optionMaxNoiseBound = app.Option("-max|--max-noise-bound", $"The max aberant data value for noise. Defaults to {dpv.MaxNoiseBound}", CommandOptionType.SingleOrNoValue);    
            
            app.Command("dps-enroll", dpsCmd =>
            {

                var pcs         = dpsCmd.Option("-p|--provisioning-connection-string","description", CommandOptionType.SingleValue).IsRequired();
                var deviceId    = dpsCmd.Option("-d|--device-id", "", CommandOptionType.SingleValue);
                var enabled     = dpsCmd.Option("-e|--enabled", $"Set the provisioning status for the device. Optional values are ({String.Join('|',Enum.GetNames(typeof(ProvisioningStatus)))})", CommandOptionType.SingleValue).Accepts(v => v.Enum<ProvisioningStatus>(ignoreCase: true));  
                var tpmkey      = dpsCmd.Option("-t|--tpm-endorsement-key", "Set a custom dps enrollment key", CommandOptionType.NoValue);
                var regId       = dpsCmd.Option("-r|--registration-id", "Set a custom registration-id", CommandOptionType.NoValue);
                
                dpsCmd.OnExecute(async () => {
                    var registrationId = "sample-registrationid-csharp";
                    var tpmEndorsementKey =
                        "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
                        "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
                        "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
                        "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
                        "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

                    var sampleDeviceId = "stableWaveFormGenerator";    
                    await EnrollDPSDevice(
                        pcs.Value(),
                        tpmkey.HasValue()   ? tpmkey.Value() : tpmEndorsementKey,
                        regId.HasValue()    ? regId.Value() : registrationId,
                        deviceId.HasValue() ? deviceId.Value() : sampleDeviceId,
                        enabled.HasValue()  ? ((ProvisioningStatus)Enum.Parse(typeof(ProvisioningStatus), optionWaveType.Value())) : ProvisioningStatus.Enabled
                    );

                    return 1;
                });
            });

            
            // null the temp dpv object 
            dpv = null;
            
            app.OnExecute(async () =>
            {

                if(args.Length > 0){
                    // not running in edge if given stand-alone flag
                    isEdgeModule = optionStandAlone.HasValue() ? false : true; 

                    desiredPropertiesData = new DesiredPropertiesData(
                        optionSendData.HasValue()       ? true                                                          : default,
                        optionSendInterval.HasValue()   ? double.Parse(optionSendInterval.Value())                      : default,
                        optionFrequency.HasValue()      ? double.Parse(optionFrequency.Value())                         : default,
                        optionAmplitude.HasValue()      ? double.Parse(optionAmplitude.Value())                         : default,
                        optionVerticalShift.HasValue()  ? double.Parse(optionVerticalShift.Value())                     : default,
                        optionWaveType.HasValue()       ? ((Waves)Enum.Parse(typeof(Waves), optionWaveType.Value()))    : Waves.Sine,
                        optionIsNoisy.HasValue()        ? bool.Parse(optionIsNoisy.Value())                             : default,
                        optionDuration.HasValue()       ? double.Parse(optionDuration.Value())                          : default,
                        optionStartValue.HasValue()     ? double.Parse(optionStartValue.Value())                        : default,
                        optionMinNoiseBound.HasValue()  ? double.Parse(optionMinNoiseBound.Value())                     : default,
                        optionMaxNoiseBound.HasValue()  ? double.Parse(optionMaxNoiseBound.Value())                     : default
                    );

                    simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);
                }
                // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
                string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

                // Cert verification is not yet fully functional when using Windows OS for the container
                bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                await Init();

                // Wait until the app unloads or is cancelled
                var cts = new CancellationTokenSource();
                AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
                await WhenCancelled(cts.Token);
            });

            return app.Execute(args);

            
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }


        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            var ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            //(desiredPropertiesData, simulatedWaveSensor) = await ParseStartupArgs(ioTHubModuleClient);                 
                
            if (null == desiredPropertiesData || null == simulatedWaveSensor)
            // callback for updating desired properties through the portal or rest api
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            if (isEdgeModule){
            // Register callback to be called when a message is received by the module
                await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", ControlMessageHandler, ioTHubModuleClient);
            }
            // as this runs in a loop we don't await
            await SendSimulationData(ioTHubModuleClient);
        }

/* 
        private static async Task<(DesiredPropertiesData, SimulatedWaveSensor)> ParseStartupArgs(DeviceClient deviceClient){
            if(Args.Length <= 0)
            {
                // if there are no commandline args then 
                var moduleTwin = await deviceClient.GetTwinAsync();
                var moduleTwinCollection = moduleTwin.Properties.Desired;
                desiredPropertiesData = new DesiredPropertiesData(moduleTwinCollection);
                simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);
                return (desiredPropertiesData, simulatedWaveSensor);

            } else {
                // we have cmd line args to parse so let's do that and 
                // report them into the desired properties for recording
                return (null, null);
            }
        }
*/
        private static Task<MessageResponse> ControlMessageHandler(Message message, object userContext)
        {
            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message Body: [{messageString}]");
            try
            {
                // Update my desired properties here
                var newDesiredProperties = JsonConvert.DeserializeObject<DesiredPropertiesData>(messageString);

                lock (PropsLocker){
                    desiredPropertiesData = newDesiredProperties;
                    simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize control command with exception: [{ex.Message}]");
            }

            return Task.FromResult(MessageResponse.Completed);

        }




        private static async Task SendSimulationData(ModuleClient deviceClient)
        {
            while(true)
            {
                try
                {
                    if(desiredPropertiesData.SendData)
                    {
                        double nextVal = simulatedWaveSensor.ReadNext();
                        MessageBody mb = buildMessage(desiredPropertiesData, nextVal);
                        var messageString = JsonConvert.SerializeObject(mb);
                        var messageBytes = Encoding.UTF8.GetBytes(messageString);
                        var message = new Message(messageBytes);
                        message.ContentEncoding = "utf-8"; 
                        message.ContentType = "application/json"; 

                        if(isEdgeModule){
                            await deviceClient.SendEventAsync("WaveForm", message);
                        } else {
                            await deviceClient.SendEventAsync(message);
                        }

                        Console.WriteLine($"\t{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToLongTimeString()}> Sending message: {counter}, Body: {messageString}");
                        Interlocked.Increment(ref counter);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(desiredPropertiesData.SendInterval));
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unexpected Exception {ex.Message}" );
                    Console.WriteLine($"\t{ex.ToString()}");
                }
            }
        }

        private static MessageBody buildMessage(DesiredPropertiesData dpd, double nextVal)
        {
            MessageBody message = new MessageBody();
            message.ReadValue = nextVal;
            message.SendData = dpd.SendData;
            message.Frequency = dpd.Frequency;
            message.SendInterval = dpd.SendInterval;
            message.VerticalShift = dpd.VerticalShift;
            message.WaveType = dpd.WaveType.ToString();
            message.Amplitude = dpd.Amplitude;
            message.ncfg.IsNoisy = dpd.IsNoisy;
            message.ncfg.Duration = dpd.Duration;
            message.ncfg.MinNoiseBound = dpd.MinNoiseBound;
            message.ncfg.MaxNoiseBound = dpd.MaxNoiseBound;
            return message;
        }

        private static Task OnDesiredPropertiesUpdate(TwinCollection twinCollection, object userContext)
        {
            lock (PropsLocker){
                desiredPropertiesData = new DesiredPropertiesData(twinCollection);
                simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);
            }
            return Task.CompletedTask;
        }

        public static async Task EnrollDPSDevice(
            string provisioningConnectionString, 
            string tpmEndorsementKey, 
            string registrationId,
            string deviceId,
            ProvisioningStatus provisioningStatus)
        {
            Console.WriteLine("Starting sample...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(provisioningConnectionString))
            {
                // create a new individualEnrollment config
                Console.WriteLine("\nCreating a new individualEnrollment...");
                Attestation attestation = new TpmAttestation(tpmEndorsementKey);
                IndividualEnrollment individualEnrollment =
                        new IndividualEnrollment(
                                registrationId,
                                attestation);

                individualEnrollment.DeviceId = deviceId;
                individualEnrollment.ProvisioningStatus = provisioningStatus;
                individualEnrollment.ETag = "*";

                // create the enrollment
                Console.WriteLine("\nAdding new individualEnrollment...");
                IndividualEnrollment individualEnrollmentResult =
                    await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                Console.WriteLine("\nIndividualEnrollment created with success.");
                Console.WriteLine(individualEnrollmentResult);


            }
        }
    }
}
