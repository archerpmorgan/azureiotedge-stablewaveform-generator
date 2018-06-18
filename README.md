# Simulated Wave Form Generator for IoT Edge


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
      "IsNoisy": false,
      "NoiseConfiguration": {
        "Start": 0,
        "Duration": 0,
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
