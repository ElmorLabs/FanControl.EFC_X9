using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FanControl.Plugins;

namespace FanControl.EFC_X9 {

    public class EFC_X9_Sensor : IPluginSensor {

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; set; }

        public int EFC_X9_Id;

        public EFC_X9_Sensor(int id1, string id2, string name) {
            EFC_X9_Id = id1;
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

        public int EFC_X9_Id;

        public delegate void FanDutyUpdatedEventHandler(EFC_X9_FanControlSensor sender, int duty);
        public event FanDutyUpdatedEventHandler? FanDutyUpdated;

        public EFC_X9_FanControlSensor(int id1, string id2, string name) {
            EFC_X9_Id = id1;
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
