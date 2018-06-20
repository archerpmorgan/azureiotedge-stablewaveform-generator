// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace WaveFormGenerator
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

        internal DesiredPropertiesData(){ }

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
    }
}