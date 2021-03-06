using System;

using System.Threading;

using System.Threading.Tasks;

namespace AzureIotEdgeSimulatedWaveSensor
{

    // Argeuments given with raised event
    public class NewDataArgs : EventArgs
    {
        public double readValue { get; set; }
        public NewDataArgs(double val){
            this.readValue = val;
        }
    }

    public class SimulatedWaveSensor : ISimulatedSensor {

        // Hypotrochoid – for simulating pumps/pistons
        // Sine – for frequency 
        // Sawtooth – for capacitance 
        // Square – for binary state transitions 
        // Triangle – just cause

        // begin customizable data parameters
        private double frequency {get; set;}  //measured in Hz
        private double period { get; set;}
        private double amplitude {get; set;} //units unspecified
        private double verticalShift {get; set;}
        private double readDelta {get; set;} // time between successive reads

        // specifies which wave form is produced on read
        private Waves waveType;

        //begin noise parameters
        private bool isNoisy {get; set;}
        private double start {get; set;}
        private double duration {get; set;}
        private double minNoiseBound {get; set;}
        private double maxNoiseBound {get; set;}
        private static readonly Random rnd = new Random();

        //keeps track of time
        private double cur;
        //event raised when neew data value occurs
        public event EventHandler<NewDataArgs> NewData;

        public SimulatedWaveSensor(DesiredPropertiesData dpd){
            Config(dpd);
            this.cur = 0; 
        }

        // set fields with values from configuration object
        public void Config(DesiredPropertiesData dpd){
            this.frequency = dpd.Frequency;
            this.period = 1 / frequency;
            this.amplitude = dpd.Amplitude;
            this.verticalShift = dpd.VerticalShift;
            this.readDelta = dpd.SendInterval;
            this.waveType = dpd.WaveType;
            this.isNoisy = dpd.IsNoisy;
            this.duration = dpd.Duration;
            this.start = dpd.Start;
            this.minNoiseBound = dpd.MinNoiseBound;
            this.maxNoiseBound = dpd.MaxNoiseBound;
        }

        //sine function translated as per the parameters specified in the instatiation of the object
        private double sine(double x) {
            double b = (2 * Math.PI) / (1 / frequency);
                return amplitude*Math.Sin(b*x) + verticalShift;
        }

        //implemented as a signed sine
        private double square(double x) {
            double b = (2 * Math.PI) / (1 / frequency);
                return amplitude*Math.Sign(Math.Sin(b*x)) + verticalShift;
        }
        //moves linearly from 0 to amplitude with a given period
        private double sawTooth(double x){
            return 2*(amplitude/period) * x - amplitude + verticalShift;
        }

        // periodic triangular wave form, translated according to
        //object specifications
        private double triangle(double x){
            if (x < period/4.0) {
                return 4 * (amplitude/period) * x + verticalShift;
            }
            else if (x < period/2.0) {
                x = x - (1/4.0*period);
                return ((-4) * (amplitude/period) * x + amplitude + verticalShift);
            }
            else if (x < 3*period/4.0) {
                x = x - (1/2.0*period);
                return (-4) * (amplitude/period) * x + verticalShift;
            }
            else {
                x = x - (3/4.0*period);
                return 4 * (amplitude/period) * x - amplitude + verticalShift;
            }
        }


        // Since each wave form is periodic, new data events are generated at standard time intervals
        // with duration specified by the sendInterval property.
        async void ReadNext(){
            while(true){
                try {
                    await Task.Delay(TimeSpan.FromSeconds(this.readDelta));
                    double val = 0;

                    switch (waveType)
                    {
                        case Waves.Sine:
                            val = sine(cur); break;
                        case Waves.Square: 
                            val = square(cur); break;
                        case Waves.Sawtooth:
                            val = sawTooth(cur); break;
                        case Waves.Triangle:
                            val = triangle(cur); break;
                        default: break;
                    }

                    // if the configuration is set accordingly, add a uniform random number to value before
                    // reporting it
                    if (isNoisy && cur >= start && cur < start + duration) {
                        val += minNoiseBound + rnd.NextDouble()*(maxNoiseBound - minNoiseBound);
                    }

                    cur = (cur + readDelta) % period;

                    //raise new data event
                    NewData?.Invoke(this, new NewDataArgs(val));

                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unexpected Exception {ex.Message}" );
                    Console.WriteLine($"\t{ex.ToString()}");
                }
            }
        }
    }
}


