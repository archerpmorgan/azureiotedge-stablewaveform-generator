// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace WaveFormGeneratorModule
{
    public class DesiredPropertiesData
    {
        private bool _sendData = true;
        private double _sendInterval = .05;
        private double _frequency = 1;
        private double _amplitude = 1;
        private double _verticalShift = 0;
        private int _waveType = 1;





        public DesiredPropertiesData(TwinCollection twinCollection)
        {
            Console.WriteLine($"Updating desired properties {twinCollection.ToJson(Formatting.Indented)}");
            try
            {
                if(twinCollection.Contains("SendData") && twinCollection["SendData"] != null)
                {
                    _sendData = twinCollection["SendData"];
                }

                if(twinCollection.Contains("SendInterval") && twinCollection["SendInterval"] != null)
                {
                    _sendInterval = twinCollection["SendInterval"];
                }

                // begin custom desired properties for wave form module

                if(twinCollection.Contains("Frequency") && twinCollection["Frequency"] != null)
                {
                    _frequency = twinCollection["Frequency"];
                }

                if(twinCollection.Contains("Amplitude") && twinCollection["Amplitude"] != null)
                {
                    _amplitude = twinCollection["Amplitude"];
                }

                if(twinCollection.Contains("VerticalShift") && twinCollection["VerticalShift"] != null)
                {
                    _verticalShift = twinCollection["VerticalShift"];
                }

                if(twinCollection.Contains("WaveType") && twinCollection["WaveType"] != null)
                {
                    _waveType = twinCollection["WaveType"];
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
                Console.WriteLine($"Value for SendData = {_sendData}");
                Console.WriteLine($"Value for SendInterval = {_sendInterval}");
                Console.WriteLine($"Value for Frequency = {_frequency}");
                Console.WriteLine($"Value for Amplitude = {_amplitude}");
                Console.WriteLine($"Value for VerticalShift = {_verticalShift}");
                Console.WriteLine($"Value for WaveType = {_waveType}");

            }
        }

        public bool SendData => _sendData;
        public double SendInterval => _sendInterval;
        public double Frequency => _frequency;
        public double Amplitude => _amplitude;
        public double VerticalShift => _verticalShift;
        public int WaveType => _waveType;
    }
}