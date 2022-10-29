using EFC_Core;
using FanControl.Plugins;
using System.Runtime.Intrinsics.X86;

namespace FanControl.EFC_X9 {
    public class EFC_X9_Plugin : IPlugin2 {
        
        public string Name { get { return "EFC-X9"; } }

        private List<Device_EFC_X9> device_list = new();
        private List<IPluginSensor> temp_sensor_list = new();
        private List<IPluginSensor> fan_speed_list = new();
        private List<IPluginControlSensor> fan_list = new();

        private readonly IPluginLogger _logger;
        private readonly IPluginDialog _dialog;

        public EFC_X9_Plugin(IPluginLogger logger, IPluginDialog dialog) {
            _logger = logger;
            _dialog = dialog;
        }

        public void Close() {
            foreach(EFC_X9_FanControlSensor fan_sensor in fan_list) {
                fan_sensor.FanDutyUpdated -= Fan_sensor_FanDutyUpdated;
            }
            fan_speed_list.Clear();
            fan_list.Clear();
            temp_sensor_list.Clear();
            foreach(Device_EFC_X9 device in device_list) {
                if(device != null) {
                    device.Disconnect();
                }
            }
            device_list.Clear();
        }

        public void Initialize() {

        }

        public void Load(IPluginSensorsContainer _container) {

            // Write log entry
            _logger.Log($"{Name} plugin Load()");

            // Find and set device
            Device_EFC_X9.GetAvailableDevices(out List<Device_EFC_X9> deviceList);

            _logger.Log($"{Name} plugin found {deviceList.Count} devices");

            // Build sensor and control lists
            fan_speed_list.Clear();
            fan_list.Clear();
            temp_sensor_list.Clear();

            List<Guid> guid_list = new List<Guid>();

            foreach(Device_EFC_X9 device in deviceList) {
                
                // Check device is connected and not already added (prevent issues when Guid is empty, FW < 3)
                if(device.Status == DeviceStatus.CONNECTED && !guid_list.Contains(device.Guid)) {

                    device_list.Add(device);
                    guid_list.Add(device.Guid);
                    
                    for(int i = 0; i < Device_EFC_X9.FAN_NUM; i++) {

                        // Fans and temperature

                        string fan_name_str = $"FAN{i + 1}";
                        string fan_id_str = $"{device.Name}.{device.Guid}.{fan_name_str}";

                        EFC_X9_FanControlSensor fan_sensor = new EFC_X9_FanControlSensor(device, i, fan_id_str, fan_name_str);
                        fan_sensor.FanDutyUpdated += Fan_sensor_FanDutyUpdated;
                        fan_list.Add(fan_sensor);

                        EFC_X9_Sensor fan_speed_sensor = new EFC_X9_Sensor(device, i, fan_id_str, fan_name_str);
                        fan_speed_list.Add(fan_speed_sensor);

                        // Temperatures

                        EFC_X9_Sensor temp_sensor;

                        // Thermistors
                        temp_sensor = new EFC_X9_Sensor(device, 0, $"{device.Name}.{device.Guid}.TS1", $"Thermistor 1 Temperature");
                        temp_sensor_list.Add(temp_sensor);
                        temp_sensor = new EFC_X9_Sensor(device, 1, $"{device.Name}.{device.Guid}.TS2", $"Thermistor 2 Temperature");
                        temp_sensor_list.Add(temp_sensor);

                        // Ambient temperature
                        temp_sensor = new EFC_X9_Sensor(device, 2, $"{device.Name}.{device.Guid}.TAMB", $"Ambient Temperature");
                        temp_sensor_list.Add(temp_sensor);

                    }
                }

                _container.TempSensors.AddRange(temp_sensor_list);
                _container.FanSensors.AddRange(fan_speed_list);
                _container.ControlSensors.AddRange(fan_list);

            }

        }

        private void Fan_sensor_FanDutyUpdated(EFC_X9_FanControlSensor sender, int duty) {
            if(sender.Device != null && sender.Device.Status == DeviceStatus.CONNECTED) {
                sender.Device.SetFanDuty(sender.DeviceId, duty);
            }
        }

        public void Update() {

            foreach(Device_EFC_X9 device in device_list) {
                if(device != null) {
                    if(device.Status != DeviceStatus.CONNECTED) {
                        // Attempt to connect
                        device.Connect();
                    }

                    if(device.Status == DeviceStatus.CONNECTED) {

                        // Update sensors
                        device.Update();

                        // Update fan speed and duties
                        for(int i = 0; i<Device_EFC_X9.FAN_NUM; i++) {

                            EFC_X9_FanControlSensor? fan_sensor = (EFC_X9_FanControlSensor?) fan_list.Find(s => s != null && s is EFC_X9_FanControlSensor && ((EFC_X9_FanControlSensor)s).Device == device && ((EFC_X9_FanControlSensor)s).DeviceId == i);

                            if(fan_sensor != null) {
                                fan_sensor.Value = (float?)device.Sensors.FanDuties[i];
                            }

                            EFC_X9_Sensor? fan_speed_sensor = (EFC_X9_Sensor?)fan_speed_list.Find(s => s != null && s is EFC_X9_Sensor && ((EFC_X9_Sensor)s).Device == device && ((EFC_X9_Sensor)s).DeviceId == i);

                            if(fan_speed_sensor != null) {
                                fan_speed_sensor.Value = (float?)device.Sensors.FanSpeeds[i];
                            }
                            
                        }

                        // Update temperatures

                        EFC_X9_Sensor? temp_sensor1 = (EFC_X9_Sensor?)temp_sensor_list.Find(s => s != null && s is EFC_X9_Sensor && ((EFC_X9_Sensor)s).Device == device && ((EFC_X9_Sensor)s).DeviceId == 0);

                        if(temp_sensor1 != null) {
                            temp_sensor1.Value = device.Sensors.Temperature1 > 1000 ? null : (float?)device.Sensors.Temperature1;
                        }

                        EFC_X9_Sensor? temp_sensor2 = (EFC_X9_Sensor?)temp_sensor_list.Find(s => s != null && s is EFC_X9_Sensor && ((EFC_X9_Sensor)s).Device == device && ((EFC_X9_Sensor)s).DeviceId == 1);

                        if(temp_sensor2 != null) {
                            temp_sensor2.Value = device.Sensors.Temperature2 > 1000 ? null : (float?)device.Sensors.Temperature2;
                        }

                        EFC_X9_Sensor? temp_sensor_ambient = (EFC_X9_Sensor?)temp_sensor_list.Find(s => s != null && s is EFC_X9_Sensor && ((EFC_X9_Sensor)s).Device == device && ((EFC_X9_Sensor)s).DeviceId == 2);

                        if(temp_sensor_ambient != null) {
                            temp_sensor_ambient.Value = device.Sensors.TemperatureAmbient > 1000 ? null : (float?)device.Sensors.TemperatureAmbient;
                        }

                    } else {

                        // Not connected, set values to null
                        foreach(EFC_X9_Sensor fan_speed in fan_speed_list) {
                            if(fan_speed.Device == device) {
                                fan_speed.Value = null;
                            }
                        }
                        foreach(EFC_X9_FanControlSensor fan in fan_list) {
                            if(fan.Device == device) {
                                fan.Value = null;
                            }
                        }
                        foreach(EFC_X9_Sensor temp_sensor in temp_sensor_list) {
                            if(temp_sensor.Device == device) {
                                temp_sensor.Value = null;
                            }
                        }

                    }
                }
            }
        }
    }
}