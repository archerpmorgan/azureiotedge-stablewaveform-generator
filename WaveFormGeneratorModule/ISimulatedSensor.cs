using System;

namespace WaveFormGenerator {
    interface ISimulatedSensor
    {
        event EventHandler<NewDataArgs> NewDataEvent;
        void Config(DesiredPropertiesData data);
    }

}