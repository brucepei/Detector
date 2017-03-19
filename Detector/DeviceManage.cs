using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Detector
{
    
    public delegate void RefreshDevicesDoneHandle();
    public class DeviceManage
    {
        public DeviceManage()
        {
            deviceList = DB.LoadDevices();
            if (deviceList == null)
            {
                deviceList = new ObservableCollection<Device>()
                {
                    new Device("CBD Server", DeviceType.IP, "cbd"),
                };
                for (int i=1; i < 10; i++)
                {
                    deviceList.Add(new Device("CBD Server" + i, DeviceType.IP, "128.0.0." + i));
                }
                Console.WriteLine("Cannot load data!");
            }
            else
            {
                Device.AutoId = MaxDeviceId() + 1;
            }
            deviceList.CollectionChanged += OnDeviceListChanged;
        }

        public MainWindow UI;
        public Int32 MaxDeviceId()
        {
            Int32 maxId = 0;
            foreach (var d in deviceList)
            {
                if (d.Id > maxId)
                {
                    maxId = d.Id;
                }
            }
            Logging.logMessage(String.Format("Get maximum device id={0}!", maxId));
            return maxId;
        }

        public void OnDeviceListChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            //ObservableCollection<Device> old = sender as ObservableCollection<Device>;
            Console.WriteLine("Device list changed: action:" + e.Action.ToString()); //Cannot print log to UI, it will cause inconsistent itemssource...
            Console.WriteLine("Device list changed: new:" + e.NewStartingIndex);
            Console.WriteLine("Device list changed: old:" + e.OldStartingIndex);
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var d = e.NewItems[0] as Device;
                DB.ChangeDB(ChangedType.APPEND, d);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var d = e.OldItems[0] as Device;
                DB.ChangeDB(ChangedType.DELETE, d);
            }
        }

        private ObservableCollection<Device> deviceList;
        public ObservableCollection<Device> DeviceList
        {
            get { return deviceList; }
            set
            {
                if (value != deviceList)
                {
                    deviceList = value;
                }
            }
        }

        public void SaveDevices()
        {
            var t = DB.SaveDataAsync();
            t.ContinueWith(task => Logging.logMessage(String.Format("DB saved: {0}!", task.Result)), TaskContinuationOptions.OnlyOnRanToCompletion);
            t.ContinueWith(task => Logging.logMessage(String.Format("DB saved exception: {0}!", task.Exception)), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void DoneRefreshDevices(List<Int32> doneList, Device device, Int32 max)
        {
            doneList.Add(device.Id);
            if (doneList.Count >= max)
            {
                Logging.logMessage("Finished refresh all devices!");
                UI.Dispatcher.BeginInvoke(UI.RepeatRefreshDevicesHandle);
            }
        }

        public void RefreshDevices()
        {
            UI = App.Current.MainWindow as MainWindow;
            var doneList = new List<Int32>();
            var totalDevices = deviceList.Count;
            for (Int32 i = 0; i < totalDevices; i++)
            {
                var device = deviceList[i];
                if (device.Type == DeviceType.IP)
                {
                    var t = Detect.PingAsync(device.IP, device);
                    t.ContinueWith(task => DoneRefreshDevices(doneList, device, totalDevices));
                }
                else if (device.Type == DeviceType.ADB_IP)
                {
                    var t = Detect.PingRemoteADBAsync(device.IP, device.ADB, device);
                    t.ContinueWith(task => DoneRefreshDevices(doneList, device, totalDevices));
                }
                Logging.logMessage(String.Format("Start to ping device {0} of id {1}", device.IP, i));
                device.Status = DeviceStatus.None;
                device.Info = "In query";
            }
        }
    }
}
