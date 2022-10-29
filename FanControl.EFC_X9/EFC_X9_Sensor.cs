using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFC_Core;
using FanControl.Plugins;

namespace FanControl.EFC_X9 {

    public class EFC_X9_Sensor : IPluginSensor {

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; set; }

        public Device_EFC_X9 Device { get; private set; }
        public int DeviceId { get; private set; }

        public EFC_X9_Sensor(Device_EFC_X9 device, int id1, string id2, string name) {
            Device = device;
            DeviceId = id1;
            Id = id2;
            Name = name;
        }

        public void Update() {
            // Use global update function instead
        }

    }

    public class EFC_X9_FanControlSensor : IPluginControlSensor {
        public string Id { get; set; }

        public string Name { get; set; }

        public float? Value { get; set; }

        public Device_EFC_X9 Device { get; private set; }
        public int DeviceId { get; private set; }

        public delegate void FanDutyUpdatedEventHandler(EFC_X9_FanControlSensor sender, int duty);
        public event FanDutyUpdatedEventHandler? FanDutyUpdated;

        public EFC_X9_FanControlSensor(Device_EFC_X9 device, int id1, string id2, string name) {
            Device = device;
            DeviceId = id1;
            Id = id2;
            Name = name;
        }

        public void Update() {
            // Use global update function instead
        }

        public void Set(float value) {
            int duty = (int) (value+0.5);
            FanDutyUpdated?.Invoke(this, duty);
        }
        
        public void Reset() {
            FanDutyUpdated?.Invoke(this, 255);
        }
    }
}
