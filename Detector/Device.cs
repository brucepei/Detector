using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Detector
{
    public enum DeviceStatus
    {
        None,
        PASS,
        FAIL,
    }

    public enum DeviceType
    {
        ADB,
        ADB_IP,
        IP,
    }
    public class Device : INotifyPropertyChanged
    {
        public static Int32 AutoId = 1;
        public Device()
        {
            id = AutoId++;
        }

        public Device(String name, DeviceType type, String ip="", String adb="")
            :this()
        {
            this.name = name;
            this.type = type;
            this.ip = ip;
            this.adb = adb;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
            DB.ChangeDB(ChangedType.MODIFY, this);
        }

        private Int32 id;
        public Int32 Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        private String name;
        public String Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        private String ip;
        public String IP
        {
            get { return ip; }
            set
            {
                if (ip != value)
                {
                    ip = value;
                    OnPropertyChanged("IP");
                }
            }
        }

        private String adb;
        public String ADB
        {
            get { return adb; }
            set
            {
                if (adb != value)
                {
                    adb = value;
                    OnPropertyChanged("ADB");
                }
            }
        }

        private String info;
        public String Info
        {
            get { return info; }
            set
            {
                if (info != value)
                {
                    info = value;
                    OnPropertyChanged("Info");
                }
            }
        }

        public String StatusImage
        {
            get { 
                String imageName = String.Empty;
                if (status == DeviceStatus.FAIL)
                {
                    imageName = "nok";
                }
                else if (status == DeviceStatus.PASS)
                {
                    imageName = "ok";
                }
                else
                {
                    imageName = "query";
                }
                return String.Format(@"images/{0}_16px.png", imageName);
            }
        }

        private DeviceStatus status;
        public DeviceStatus Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged("StatusImage");
                }
            }
        }

        private DeviceType type;
        public DeviceType Type
        {
            get { return type; }
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged("Type");
                }
            }
        }
    }
}
