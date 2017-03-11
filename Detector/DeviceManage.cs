using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;


namespace Detector
{
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
                Console.WriteLine("Cannot load data!");
            }
            int maxId = 0;
            foreach (var d in deviceList)
            {
                if (d.Id > maxId)
                {
                    maxId = d.Id;
                }
            }
            Device.AutoId = maxId + 1;
            deviceList.CollectionChanged += OnDeviceListChanged;
        }

        public void SaveData(Object sender, EventArgs e)
        {
            Logging.logMessage("device saved!");
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

    }
}
