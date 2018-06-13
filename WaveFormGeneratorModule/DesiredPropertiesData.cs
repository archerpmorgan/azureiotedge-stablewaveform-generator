// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace AzureIotEdgeSimulatedWaveSensor
{
    public enum Waves {
        Sine,
        Sawtooth,
        Square,
        Triangle
    }

    public class DesiredPropertiesData
    {
        public bool SendData {get; private set;}            = true;
        public double SendInterval {get; private set;}      = .05;
        public double Frequency {get; private set;}         = 1;
        public double Amplitude {get; private set;}         = 1;
        public double VerticalShift {get; private set;}     = 0;
        public Waves WaveType {get; private set;}           = Waves.Sine;

        // begin noise parameters

        public bool IsNoisy {get; private set;}            = false;
        public double Start {get; private set;}            = 0;
        public double Duration {get; private set;}         = 0;
        public double MinNoiseBound {get; private set;}    = 0;
        public double MaxNoiseBound {get; private set;}    = 0;

        public DesiredPropertiesData(
            bool sendData, double sendInterval, double frequency,
            double amplitude, double verticalShift, Waves waveType,
            bool isNoisy, double start, double duration,
            double minNoiseBound, double maxNoiseBound)
        {
            SendData            =   sendData;
            SendInterval        =   sendInterval;
            Frequency           =   frequency;
            Amplitude           =   amplitude;
            VerticalShift       =   verticalShift;
            WaveType            =   waveType;
            IsNoisy             =   isNoisy;
            Duration            =   duration;
            Start               =   start;
            MinNoiseBound       =   minNoiseBound;
            MaxNoiseBound       =   maxNoiseBound;
        }

        public DesiredPropertiesData(TwinCollection twinCollection)
        {
            Console.WriteLine($"Updating desired properties {twinCollection.ToJson(Formatting.Indented)}");
            try
            {
                if(twinCollection.Contains("SendData") && twinCollection["SendData"] != null)
                {
                    SendData = twinCollection["SendData"];
                }

                if(twinCollection.Contains("SendInterval") && twinCollection["SendInterval"] != null)
                {
                    SendInterval = twinCollection["SendInterval"];
                }

                // begin custom desired properties for wave form module

                if(twinCollection.Contains("Frequency") && twinCollection["Frequency"] != null)
                {
                    Frequency = twinCollection["Frequency"];
                }

                if(twinCollection.Contains("Amplitude") && twinCollection["Amplitude"] != null)
                {
                    Amplitude = twinCollection["Amplitude"];
                }

                if(twinCollection.Contains("VerticalShift") && twinCollection["VerticalShift"] != null)
                {
                    VerticalShift = twinCollection["VerticalShift"];
                }

                if(twinCollection.Contains("WaveType") && twinCollection["WaveType"] != null)
                {
                    WaveType = (Waves)Enum.Parse(typeof(Waves), twinCollection["WaveType"]);
                }
                if(twinCollection.Contains("IsNoisy") && twinCollection["IsNoisy"] != null)
                {
                    IsNoisy = twinCollection["IsNoisy"];
                }
                if(twinCollection.Contains("Duration") && twinCollection["Duration"] != null)
                {
                    Duration = twinCollection["Duration"];
                }
                if(twinCollection.Contains("Start") && twinCollection["Start"] != null)
                {
                    Start = twinCollection["Start"];
                }
                if(twinCollection.Contains("MinNoiseBound") && twinCollection["MinNoiseBound"] != null)
                {
                    MinNoiseBound = twinCollection["MinNoiseBound"];
                }
                if(twinCollection.Contains("MaxNoiseBound") && twinCollection["MaxNoiseBound"] != null)
                {
                    MaxNoiseBound = twinCollection["MaxNoiseBound"];
                }
            }
            catch(AggregateException aexc)
            {
                foreach(var exception in aexc.InnerExceptions)
                {
                    Console.WriteLine($"[ERROR] Could not retrieve desired properties {aexc.Message}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[ERROR] Reading desired properties failed with {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Value for SendData = {SendData}");
                Console.WriteLine($"Value for SendInterval = {SendInterval}");
                Console.WriteLine($"Value for Frequency = {Frequency}");
                Console.WriteLine($"Value for Amplitude = {Amplitude}");
                Console.WriteLine($"Value for VerticalShift = {VerticalShift}");
                Console.WriteLine($"Value for WaveType = {WaveType}");

            }
        }
    }
}