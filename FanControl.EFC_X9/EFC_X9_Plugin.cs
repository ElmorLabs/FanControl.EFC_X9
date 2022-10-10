using EFC_Core;
using FanControl.Plugins;

namespace FanControl.EFC_X9 {
    public class EFC_X9_Plugin : IPlugin2 {
        
        public string Name { get { return "EFC-X9"; } }

        private Device_EFC_X9_V1? efc_device;
        private List<IPluginSensor> temp_sensor_list = new();
        private List<IPluginSensor> fan_speed_list = new();
        private List<IPluginControlSensor> fan_list = new();

        public void Close() {
            temp_sensor_list.Clear();
            //fan_speed_list.Clear();
            fan_list.Clear();
            if(efc_device != null) {
                efc_device.Disconnect();
                efc_device = null;
            }
        }

        public void Initialize() {

        }

        public void Load(IPluginSensorsContainer _container) {

            // Find and set device
            Device_EFC_X9_V1.GetAvailableDevices(out List<Device_EFC_X9_V1> deviceList);

            if(deviceList.Count > 0) {
                // TODO: support more than first device
                efc_device = deviceList[0];
            }

            if(efc_device != null && efc_device.Status == DeviceStatus.CONNECTED) {

                // Build sensor and control lists

                // Fans
                fan_speed_list.Clear();
                fan_list.Clear();

                for(int i = 0; i<Device_EFC_X9_V1.FAN_NUM; i++) {
                    string fan_name_str = $"FAN{i + 1}";
                    string fan_id_str = $"{Name}.0.{fan_name_str}";

                    EFC_X9_FanControlSensor fan_sensor = new EFC_X9_FanControlSensor(i, fan_id_str, fan_name_str);
                    fan_sensor.FanDutyUpdated += Fan_sensor_FanDutyUpdated;
                    fan_list.Add(fan_sensor);

                    EFC_X9_Sensor fan_speed_sensor = new EFC_X9_Sensor(i, fan_id_str, fan_name_str);
                    fan_speed_list.Add(fan_speed_sensor);

                }

                // Temperatures
                temp_sensor_list.Clear();

                EFC_X9_Sensor temp_sensor;

                // Thermistors
                temp_sensor = new EFC_X9_Sensor(0, $"{Name}.0.TS1", $"Thermistor 1 Temperature");
                temp_sensor_list.Add(temp_sensor);
                temp_sensor = new EFC_X9_Sensor(1, $"{Name}.0.TS2", $"Thermistor 2 Temperature");
                temp_sensor_list.Add(temp_sensor);

                // Ambient temperature
                temp_sensor = new EFC_X9_Sensor(2, $"{Name}.0.TAMB", $"Ambient Temperature");
                temp_sensor_list.Add(temp_sensor);

                _container.TempSensors.AddRange(temp_sensor_list);
                _container.FanSensors.AddRange(fan_speed_list);
                _container.ControlSensors.AddRange(fan_list);

            }

        }

        private void Fan_sensor_FanDutyUpdated(EFC_X9_FanControlSensor sender, int duty) {
            if(efc_device != null && efc_device.Status == DeviceStatus.CONNECTED) {
                efc_device.SetFanDuty(sender.EFC_X9_Id, duty);
            }
        }

        public void Update() {
            if(efc_device != null) {

                if(efc_device.Status != DeviceStatus.CONNECTED) {
                    // Attempt to connect
                    efc_device.Connect();
                }

                if(efc_device.Status == DeviceStatus.CONNECTED) {

                    // Update sensors
                    efc_device.Update();

                    // Update fan speed and duties
                    for(int i = 0; i < fan_list.Count; i++) {

                        EFC_X9_FanControlSensor fan_sensor = (EFC_X9_FanControlSensor)fan_list[i];
                        fan_sensor.Value = (float?)efc_device.Sensors.FanDuties[i];

                        EFC_X9_Sensor fan_speed_sensor = (EFC_X9_Sensor)fan_speed_list[i];
                        fan_speed_sensor.Value = (float?)efc_device.Sensors.FanSpeeds[i];
                    }

                    // Update temperatures

                    EFC_X9_Sensor temp_sensor;

                    temp_sensor = (EFC_X9_Sensor)temp_sensor_list[0];
                    temp_sensor.Value = efc_device.Sensors.Temperature1 > 1000 ? null : (float?)efc_device.Sensors.Temperature1;

                    temp_sensor = (EFC_X9_Sensor)temp_sensor_list[1];
                    temp_sensor.Value = efc_device.Sensors.Temperature2 > 1000 ? null : (float?)efc_device.Sensors.Temperature2;

                    temp_sensor = (EFC_X9_Sensor)temp_sensor_list[2];
                    temp_sensor.Value = efc_device.Sensors.TemperatureAmbient > 1000 ? null : (float?)efc_device.Sensors.TemperatureAmbient;
                }
                
                return;

            }

            // Not connected, set values to null
            foreach(EFC_X9_Sensor fan_speed in fan_speed_list) {
                fan_speed.Value = null;
            }
            foreach(EFC_X9_FanControlSensor fan in fan_list) {
                fan.Value = null;
            }
            foreach(EFC_X9_Sensor temp_sensor in temp_sensor_list) {
                temp_sensor.Value = null;
            }
        }
    }
}