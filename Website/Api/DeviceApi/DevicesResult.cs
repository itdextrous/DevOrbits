using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class DevicesResult
    {
        public Device[] devices { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public int count { get; set; }
        public int total { get; set; }
    }

    public class Device : ICloneable
    {
        public int id { get; set; }
        public string name { get; set; }
        public string serial { get; set; }
        public string reference { get; set; }
        public MeterType type { get; set; }
        public Status status { get; set; }
        public DeviceMapping mapping { get; set; }
        public string gps_coordinates { get; set; }
        public string partner_code { get; set; }
        public string customer_number { get; set; }
        public string balance { get; set; }
        public string account { get; set; }
        public bool mapClickable { get; set; }
        public string account_type { get; set; }
        public bool autoDisconnect { get; set; }
        public string deviceType
        {
            get
            {
                if (type != null)
                {
                    return type.type;
                }
                return "";
            }
        }
        public string deviceStatus
        {
            get
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.ToUpper().Contains("KVA")
                        || name.ToUpper().Contains("OFFPEAK")
                        || name.ToUpper().Contains("PEAK")
                        || name.ToUpper().Contains("STANDARD"))
                    {
                        status = new Status()
                        {
                            id = 1,
                            time = DateTime.Now,
                        };
                        return "online";
                    }

                }
                if (status != null)
                {
                    if (status.id == 1)
                    {
                        return "online";
                    }
                    else if (status.id == 2)
                    {
                        return "offline";
                    }
                    return "offline";
                }
                return "offline";
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    public class Status
    {
        public int id { get; set; }
        public DateTime? time { get; set; }
    }

    public class DeviceMapping
    {
        public int index { get; set; }
        public int port { get; set; }
        public int protocol { get; set; }
        public int remote_address { get; set; }
        public int remote_index { get; set; }
        public int process_interval { get; set; }
        public int status { get; set; }
        public string last_communicated { get; set; }
    }

    public class DeviceUpdate
    {
        public string name { get; set; }
    }


    public class DeviceUpdateResult
    {
        public string result { get; set; }
        public Device device { get; set; }
        public class Device
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public Status status { get; set; }
            public Type type { get; set; }
        }

        public class Status
        {
            public int id { get; set; }
            public DateTime time { get; set; }
        }

        public class Type
        {
            public int id { get; set; }
            public string name { get; set; }
        }
    }



    public class CreateOrUpdateM2MDevice
    {
        public Device[] devices { get; set; }
        public class Device
        {
            public int id { get; set; }
            public string serial { get; set; }
            public string name { get; set; }
            public int type_id { get; set; }
            public Mapping mapping { get; set; }
        }

        public class Mapping
        {
            public int port { get; set; }
            public int protocol_id { get; set; }
            public string remote_address { get; set; }
            public int remote_index { get; set; }
            public int process_interval { get; set; }
        }
    }


    public class CreateOrUpdateM2MDeviceResult
    {
        public string result { get; set; }
        public Command command { get; set; }
        public class Command
        {
            public int id { get; set; }
            public int command { get; set; }
            public object number { get; set; }
            public int action { get; set; }
            public int status { get; set; }
            public DateTime request_time { get; set; }
            public string request_data { get; set; }
            public object response_time { get; set; }
            public object response_data { get; set; }
        }

    }



    public class CreateOrUpdateM2MDeviceConfig
    {
        public Action action { get; set; }
        public class Action
        {
            public int id { get; set; }
            public string value { get; set; }
        }
    }

    #region DeviceGatewaysAndMapping


    public class DeviceGatewaysAndMapping
    {
        public Device device { get; set; }

        public class Device
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public Status status { get; set; }
            public Type type { get; set; }
            public Configuration[] configurations { get; set; }
            public Gateway[] gateways { get; set; }
        }

        public class Status
        {
            public int id { get; set; }
            public DateTime time { get; set; }
        }

        public class Type
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Configuration
        {
            public int id { get; set; }
            public int number { get; set; }
            public string name { get; set; }
            public int format { get; set; }
            public Value value { get; set; }
        }

        public class Value
        {
            public string value { get; set; }
            public object status { get; set; }
        }

        public class Gateway
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public Status1 status { get; set; }
            public Mapping mapping { get; set; }
            public Type1 type { get; set; }
        }

        public class Status1
        {
            public int id { get; set; }
            public DateTime time { get; set; }
        }

        public class Mapping
        {
            public int index { get; set; }
            public int port { get; set; }
            public int protocol { get; set; }
            public int remote_address { get; set; }
            public int remote_index { get; set; }
            public int process_interval { get; set; }
            public DateTime last_communicated { get; set; }
        }

        public class Type1
        {
            public int id { get; set; }
            public string name { get; set; }
        }

    }


    #endregion

    #region ConfigValue


    public class ConfigValue
    {
        public Device device { get; set; }
        public class Device
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public Status status { get; set; }
            public Type type { get; set; }
            public Control[] controls { get; set; }
            public Configuration[] configurations { get; set; }
        }

        public class Status
        {
            public int id { get; set; }
            public DateTime time { get; set; }
        }

        public class Type
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Control
        {
            public int id { get; set; }
            public int number { get; set; }
            public string name { get; set; }
            public int status { get; set; }
            public Action[] actions { get; set; }
        }

        public class Action
        {
            public int id { get; set; }
            public int number { get; set; }
            public string name { get; set; }
            public bool parameter { get; set; }
            public int format { get; set; }
            public int size { get; set; }
        }

        public class Configuration
        {
            public int id { get; set; }
            public int number { get; set; }
            public string name { get; set; }
            public int format { get; set; }
            public Value value { get; set; }
        }

        public class Value
        {
            public object value { get; set; }
            public object status { get; set; }
        }

    }


    #endregion




    public class GetDeviceByIDResult
    {
        public Device device { get; set; }
        public class Device
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public Model model { get; set; }
            public Status status { get; set; }
            public Type type { get; set; }
            public string deviceStatus
            {
                get
                {
                    if (name.ToUpper().Contains("KVA")
                        || name.ToUpper().Contains("OFFPEAK")
                        || name.ToUpper().Contains("PEAK")
                        || name.ToUpper().Contains("STANDARD"))
                        return "online";

                    if (status != null)
                    {
                        if (status.id == 1)
                        {
                            return "online";
                        }
                        else if (status.id == 2)
                        {
                            return "offline";
                        }
                        return "";
                    }
                    return "";
                }
            }
        }

        public class Status
        {
            public int id { get; set; }
            public DateTime? time { get; set; }
        }

        public class Type
        {
            public int id { get; set; }
            public string name { get; set; }
            public string UnitType
            {
                get
                {
                    if (id == (Int32)DeviceTypeOptions.WirelessPulseInput || id == (Int32)DeviceTypeOptions.WaterMeter)
                    {
                        return "litres";
                    }
                    else if (id == (Int32)DeviceTypeOptions.GasMeter)
                    {
                        return "m3";
                    }
                    else if (id == (Int32)DeviceTypeOptions.GasModem)
                    {
                        return "m3";
                    }
                    else if (id == (Int32)DeviceTypeOptions.ControlValve)
                    {
                        return "";
                    }

                    return "units";
                }
            }
        }
        public class Model
        {
            public int id { get; set; }
            public string name { get; set; }
            public string group { get; set; }
        }
    }


    public class DevicesResultApi2
    {
        public Device[] devices { get; set; }
        public Meta meta { get; set; }
        public class Meta
        {
            public object limit { get; set; }
            public object offset { get; set; }
            public int count { get; set; }
            public int total { get; set; }
        }

        public class Device
        {
            public int id { get; set; }
            public string name { get; set; }
            public string serial { get; set; }
            public object reference { get; set; }
            public Model model { get; set; }
            public Type type { get; set; }
            public Status status { get; set; }
            public string deviceType
            {
                get
                {
                    if (type != null)
                    {
                        return type.name;
                    }
                    return "";
                }
            }
            public string deviceStatus
            {
                get
                {
                    if (name.ToUpper().Contains("KVA")
                        || name.ToUpper().Contains("OFFPEAK")
                        || name.ToUpper().Contains("PEAK")
                        || name.ToUpper().Contains("STANDARD"))
                    {
                        status = new Status()
                        {
                            id = 1,
                            time = DateTime.Now,
                        };
                        return "online";
                    }

                    if (status != null)
                    {
                        if (status.id == 1)
                        {
                            return "online";
                        }
                        else if (status.id == 2)
                        {
                            return "offline";
                        }
                        return "";
                    }
                    return "";
                }
            }
        }

        public class Model
        {
            public int id { get; set; }
            public string name { get; set; }
            public string group { get; set; }
        }

        public class Type
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Status
        {
            public int id { get; set; }
            public DateTime? time { get; set; }
        }
    }


}
