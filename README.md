# Simulated Wave Form Generator for IoT Edge

This project is a stable wave form generator to be used for general IoT data generation.  Key scenarios for this generator include Machine Learning, Predictive Maintenance & Remote Monitoring.  This module has several forms of deployment including: console application, IoT Edge Module, Containerized Console and as a reference library (Nuget package).  

## Wave Form
This application generates four (4) standtard wave forms including 
- Sine (default)
- Square
- Triangular
- Sawtooth

## Configuration
The application/library's wave functions are parameterized by a number of values:
- frequency: the length of a single wave in seconds.
- amplitude: the measure of the waves center line to it's peak value.
- vertical shift: the positive or negative offset from 0, along the y-axis, to offset the wave.
- send interval: the frequency to read values from the wave function. This value determines the output velocity.
- wave type: see [wave forms](#wave-forms) above.   

## Noise
The application/library supports the injection of noise into the stable wave form to simulate triggers for machine learning or predictive maintanence models.  Noise is controlled by the IoT Edge Module's Device Twin or via console args.  Noise is specified via: 
- start period: time value from the start of a wave. 
- duration: the length of noise to generate.
- min noise bound: the lower threshold of random noise generation.
- max noise bound: the upper threshold of random noise generation. 

Below are the desired properties in Json used by the module:

```json
  "properties": {
    "desired": {
      "SendData": true,
      "Frequency": 1,
      "Amplitude": 1,
      "VerticalShift": 0,
      "SendInterval": 0.05,
      "WaveType": "Sine",
      "NoiseConfiguration": {
        "Start": 0,
        "Duration": 0,
        "IsNoisy": false,
        "MinNoiseBound": 0,
        "MaxNoiseBound": 0
      }
    }
  }
```

| Twin Property  | Description |
| ------------- | ------------- |
| SendData  | starts or stops pushing messages to the output endpoint  |
| SendInterval  | the amount of time in seconds between value publishes  |
| Frequency  | the frequency of the wave in Hz  |
| Amplitude  | the aplitude of the wave |
| VerticalShift  | shift the wave vertically by some constant amount  |
| WaveType  | tells the simulator which type of wave to produce  |


[TPM Simulator](https://www.microsoft.com/en-us/download/confirmation.aspx?id=52507)
[TPM Project Page](https://www.microsoft.com/en-us/research/project/the-trusted-platform-module-tpm/)