using System;

namespace AzureIotEdgeSimulatedWaveSensor
{
    public class SimulatedWaveSensor {

        // Hypotrochoid – for simulating pumps/pistons
        // Sine – for frequency 
        // Sawtooth – for capacitance 
        // Square – for binary state transitions 
        // Triangle – just cause
        // Trochoid – cause everyone loves a wankel rotary engine. 

        // begin customizable data parameters
        private double frequency {get; set;}  //measured in Hz
        private double period { get; set;}
        private double amplitude {get; set;} //units unspecified
        private double verticalShift {get; set;}
        private double readDelta {get; set;} // time between successive reads

        // specifies which wave form is produced on read
        // 1 - Sine
        // 2 - Square
        // 3 - SawTooth
        // 4 - Triangle
        private int waveType;

        //begin noise parameters
        private bool isNoisy {get; set;}
        private double start {get; set;}
        private double duration {get; set;}
        private double minNoiseBound {get; set;}
        private double maxNoiseBound {get; set;}

        private double cur;

        private static readonly Random rnd = new Random();

        public SimulatedWaveSensor(double freq, double amp, double vert, double delta, int type, bool isNoisy, double duration, double start, double min, double max){
            this.frequency = freq;
            this.period = 1 / frequency;
            this.amplitude = amp;
            this.verticalShift = vert;
            this.readDelta = delta;
            this.waveType = type;
            this.isNoisy = isNoisy;
            this.duration = duration;
            this.start = start;
            this.minNoiseBound = min;
            this.maxNoiseBound = max;

            this.cur = 0; 
        }

        public SimulatedWaveSensor(DesiredPropertiesData dpd){
            this.frequency = dpd.Frequency;
            this.period = 1 / frequency;
            this.amplitude = dpd.Amplitude;
            this.verticalShift = dpd.VerticalShift;
            this.readDelta = dpd.SendInterval;
            this.waveType = dpd.WaveType;

            this.cur = 0;
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

        // returns an array of 1000 doubles generated by the strategy
        public double ReadNext(){

            double retval = 0;

            switch (waveType)
            {
                case 1:
                    retval = sine(cur); break;
                case 2: 
                    retval = square(cur); break;
                case 3:
                    retval = sawTooth(cur); break;
                case 4:
                    retval = triangle(cur); break;
                default: break;
            }

            // if the configuration is set accordingly, add a uniform random number to value before
            // reporting it
            if (isNoisy && cur >= start && cur < start + duration) {
                retval += minNoiseBound + rnd.NextDouble()*(maxNoiseBound - minNoiseBound);
            }

            cur = (cur + readDelta) % period;
            return retval;
        }
    }
} 