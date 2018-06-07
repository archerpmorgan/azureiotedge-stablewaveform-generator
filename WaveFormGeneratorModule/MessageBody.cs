namespace AzureIotEdgeSimulatedWaveSensor {

    public class MessageBody{

        public double ReadValue { get; set; }
       
        public bool SendData { get; set; }
        public double SendInterval { get; set; }
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public double VerticalShift{ get; set; }
        public int WaveType { get; set; }
        public NoiseConfiguration ncfg = new NoiseConfiguration();
    }

    public class NoiseConfiguration {

        public bool IsNoisy { get; set; }
        public double Start { get; set; }
        public double Duration { get; set; }
        public double MinNoiseBound { get; set; }
        public double MaxNoiseBound { get; set; }

    }

}