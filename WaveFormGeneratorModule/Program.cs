namespace WaveFormGeneratorModule
{

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
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


    class Program
    {
        static object PropsLocker = new object();
        static int counter;
        private static DesiredPropertiesData desiredPropertiesData;
        private static SimulatedWaveSensor simulatedWaveSensor;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication<Program>();

            app.HelpOption();

            var optionSendData =        app.Option("-s|--send-data", "Enable sending of data", CommandOptionType.NoValue);    
            var optionSendInterval =    app.Option("-si|--send-interval <INTERVAL>", "The interval to send data. Defaults to 5 seconds.", CommandOptionType.SingleOrNoValue);
            var optionFrequency =       app.Option("-f|--frequency", "Frequency of wave measureing reading. Defaults to 5 seconds (0.2hz).", CommandOptionType.SingleOrNoValue);
            var optionAmplitude =       app.Option("-a|--amplitude", "Amplitude of the wave.", CommandOptionType.SingleOrNoValue);    
            var optionVerticalShift =   app.Option("-vs|--vertical-shift", "Positive or negative offset.", CommandOptionType.SingleOrNoValue);    
            var optionWaveType =        app.Option("-wt|--wave-type", "(Sine|Square|SawTooth|Triangle)", CommandOptionType.SingleOrNoValue).Accepts(v => v.Values("Sine","Square","SawTooth","Triangle"));    
            var optionIsNoisy =         app.Option("-n|--is-noisy", "If present, indicates aberations in value output.", CommandOptionType.NoValue);    
            var optionDuration =        app.Option("-d|--duration", "Lenght of noise generation in seconds. If 'is noisy', defaults to 1 second (1hz).", CommandOptionType.SingleOrNoValue);    
            var optionStartValue =      app.Option("-v|--start", "", CommandOptionType.SingleOrNoValue);    
            var optionMinNoiseBound =   app.Option("-min|--min-noise-bound", "", CommandOptionType.SingleOrNoValue);    
            var optionMaxNoiseBound =   app.Option("-max|--max-noise-bound", "", CommandOptionType.SingleOrNoValue);    
            
            app.OnExecute(async () =>
            {
                if(args.Length > 0){
                    desiredPropertiesData = new DesiredPropertiesData(
                        optionSendData.HasValue() ? bool.Parse(optionSendData.Value()) : default,
                        optionSendInterval.HasValue() ? double.Parse(optionSendInterval.Value()) : default,
                        optionFrequency.HasValue() ? double.Parse(optionFrequency.Value()) : default,
                        optionAmplitude.HasValue() ? double.Parse(optionAmplitude.Value()) : default,
                        optionVerticalShift.HasValue() ? double.Parse(optionVerticalShift.Value()) : default,
                        optionWaveType.HasValue() ? ((Waves)Enum.Parse(typeof(Waves), optionWaveType.Value())) : Waves.Sine,
                        optionIsNoisy.HasValue() ? bool.Parse(optionIsNoisy.Value()) : default,
                        optionDuration.HasValue() ? double.Parse(optionDuration.Value()) : default,
                        optionStartValue.HasValue() ? double.Parse(optionStartValue.Value()) : default,
                        optionMinNoiseBound.HasValue() ? double.Parse(optionMinNoiseBound.Value()) : default,
                        optionMaxNoiseBound.HasValue() ? double.Parse(optionMaxNoiseBound.Value()) : default
                    );
                }
                // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
                string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

                // Cert verification is not yet fully functional when using Windows OS for the container
                bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if (!bypassCertVerification) InstallCert();
                await Init(connectionString, bypassCertVerification);

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
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }









        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            if (bypassCertVerification)
            {
                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            DeviceClient ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            //(desiredPropertiesData, simulatedWaveSensor) = await ParseStartupArgs(ioTHubModuleClient);                 
                

            // callback for updating desired properties through the portal or rest api
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", ControlMessageHandler, ioTHubModuleClient);

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




        private static async Task SendSimulationData(DeviceClient deviceClient)
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

                        await deviceClient.SendEventAsync("WaveFormOutput", message);
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
    }
}
