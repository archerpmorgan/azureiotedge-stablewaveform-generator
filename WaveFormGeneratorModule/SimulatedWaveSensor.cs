using System;

namespace AzureIotEdgeSimulatedWaveSensor
{
    public class WaveSensor {

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
        private double cur;

        // specifies which wave form is produced on read
        // 1 - Sine
        // 2 - Square
        // 3 - SawTooth
        // 1 - Triangle
        private int waveType;

        public WaveSensor(double freq, double amp, double vert, double delta, int type){
            this.frequency = freq;
            this.period = 1 / frequency;
            this.amplitude = amp;
            this.verticalShift = vert;
            this.readDelta = delta;
            this.waveType = type;
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
            x = x % period;
            return (amplitude/period) * x + verticalShift;
        }

        // periodic triangular wave form, translated according to
        //object specifications
        private double triangle(double x){
            x = x % period;
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

            // map wave transformations onto array of read times
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

            cur += readDelta;
            return retval;
        }
    }
} 