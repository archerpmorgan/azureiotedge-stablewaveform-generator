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

// // disabling async warning as the SendSimulationData is an async method
// // but we don't wait for it
#pragma warning disable CS4014


    class Program
    {
        static int counter;
        private static volatile DesiredPropertiesData desiredPropertiesData;
        private static SimulatedWaveSensor simulatedWaveSensor;



        // static async Task Main(string[] args)
        // {
        //     // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
        //     var connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

        //     // Cert verification is not yet fully functional when using Windows OS for the container
        //     var bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        //     if (!bypassCertVerification) InstallCert();
        //     await Init(connectionString, bypassCertVerification);

        //     // Wait until the app unloads or is cancelled
        //     var cts = new CancellationTokenSource();
        //     AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
        //     Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
        //     await WhenCancelled(cts.Token);
        // }

                static void Main(string[] args)
        {
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
            string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!bypassCertVerification) InstallCert();
            Init(connectionString, bypassCertVerification).Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
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

            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            var moduleTwinCollection = moduleTwin.Properties.Desired;
            desiredPropertiesData = new DesiredPropertiesData(moduleTwinCollection);
            simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);

            // callback for updating desired properties through the portal or rest api
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", ControlMessageHandler, ioTHubModuleClient);

            // as this runs in a loop we don't await
            SendSimulationData(ioTHubModuleClient);
        }



        private static Task<MessageResponse> ControlMessageHandler(Message message, object userContext)
        {
            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message Body: [{messageString}]");
            try
            {
                // Update my desired properties here
                var NewDesiredProperties = JsonConvert.DeserializeObject<DesiredPropertiesData>(messageString);
                desiredPropertiesData = NewDesiredProperties;
                simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);
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
            message.WaveType = dpd.WaveType;
            message.Amplitude = dpd.Amplitude;
            message.ncfg.IsNoisy = dpd.IsNoisy;
            message.ncfg.Duration = dpd.Duration;
            message.ncfg.MinNoiseBound = dpd.MinNoiseBound;
            message.ncfg.MaxNoiseBound = dpd.MaxNoiseBound;
            return message;
        }

        private static Task OnDesiredPropertiesUpdate(TwinCollection twinCollection, object userContext)
        {
            desiredPropertiesData = new DesiredPropertiesData(twinCollection);
            simulatedWaveSensor = new SimulatedWaveSensor(desiredPropertiesData);

            return Task.CompletedTask;
        }
    }
}
