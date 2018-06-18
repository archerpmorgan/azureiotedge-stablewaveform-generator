using System;

namespace  AzureIotEdgeSimulatedWaveSensor {
    interface ISimulatedSensor
    {
        event EventHandler<NewDataArgs> NewData;
        void Config(DesiredPropertiesData data);
    }

}