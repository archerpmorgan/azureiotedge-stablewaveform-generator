using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveFormGenerator
{
    interface ISimulatedSensor
    {
        event EventHandler<NewDataArgs> NewDataEvent;
        void Config(DesiredPropertiesData data);
    }
}
