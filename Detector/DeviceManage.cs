﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows;

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
                for (int i=1; i < 32; i++)
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
        public readonly Int32 MaxConcurrentTask = 5;
        private ConcurrentQueue<Int32> resumeQueue;
        private ConcurrentQueue<Int32> doneQueue;
        private Int32 TaskinQueue = 0;

        public MainWindow UI;
        public string ASCommand;
        public int DefaultASTimeout = 5000;
        public int ASTimeout;
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
            t.ContinueWith(task => Logging.logMessage(String.Format("DB saved with exception: {0}!", task.Exception)), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void DoneRefreshDevices(Int32 deviceId)
        {
            doneQueue.Enqueue(deviceId);
            Logging.logMessage("Device id " + deviceId + " done!");
            if (doneQueue.Count >= TaskinQueue)
            {
                Logging.logMessage("Finished refresh all devices!");
                UI.Dispatcher.BeginInvoke(UI.RepeatRefreshDevicesHandle);
            }
            else
            {
                StartRefreshTask();
            }
        }

        public void StartRefreshTask()
        {
            if (!resumeQueue.IsEmpty) 
            {
                Int32 deviceIndex;
                if (resumeQueue.TryDequeue(out deviceIndex))
                {
                    var device = deviceList[deviceIndex];
                    Logging.logMessage("Got task with device id: " + device.Id);
                    device.Status = DeviceStatus.QUERY;
                    device.Info = "Query...";
                    if (device.Type == DeviceType.AS_IP)
                    {
                        var t = device.DetectAsync(ASCommand, ASTimeout);
                        if (t != null)
                        {
                            t.ContinueWith(task => DoneRefreshDevices(device.Id));
                        }
                    }
                    else
                    {
                        var t = device.DetectAsync();
                        if (t != null)
                        {
                            t.ContinueWith(task => DoneRefreshDevices(device.Id));
                        }
                    }
                }
                else
                {
                    Logging.logMessage("No device to deque!");
                }
            }
        }

        public void DetectFailedDevices(MainWindow ui)
        {
            UI = ui;
            ASCommand = ui.asCommand.Text;
            int asTimeout = 0;
            if (Int32.TryParse(ui.asTimeout.Text, out asTimeout))
            {
                ASTimeout = asTimeout;
            }
            else
            {
                ASTimeout = DefaultASTimeout;
            }
            if (resumeQueue != null && !resumeQueue.IsEmpty)
            {
                Logging.logMessage("Refresh is ongoing, cannot detect failed device!");
                return;
            }
            resumeQueue = new ConcurrentQueue<Int32>();
            doneQueue = new ConcurrentQueue<Int32>();
            TaskinQueue = deviceList.Count;
            for (Int32 i = 0; i < deviceList.Count; i++)
            {
                var device = deviceList[i];
                if (device.Status == DeviceStatus.FAIL)
                {
                    resumeQueue.Enqueue(i);
                    device.Status = DeviceStatus.QUEUE;
                    device.Info = "In Queue";
                }
            }
            if (resumeQueue.Count > 0)
            {
                Logging.logMessage(String.Format("Found {0} failed device!", resumeQueue.Count));
                for (Int32 i = 0; i < MaxConcurrentTask; i++)
                {
                    Logging.logMessage("Start task: " + i);
                    StartRefreshTask();
                }
            }
            else
            {
                Logging.logMessage("No failed device!");
            }
        }

        public void RefreshDevices(MainWindow ui)
        {
            UI = ui;
            ASCommand = ui.asCommand.Text;
            int asTimeout = 0;
            if (Int32.TryParse(ui.asTimeout.Text, out asTimeout))
            {
                ASTimeout = asTimeout;
            }
            else
            {
                ASTimeout = DefaultASTimeout;
            }
            if (resumeQueue != null && !resumeQueue.IsEmpty)
            {
                Logging.logMessage("Refresh is ongoing, cannot refresh again!");
                return;
            }
            resumeQueue = new ConcurrentQueue<Int32>();
            doneQueue = new ConcurrentQueue<Int32>();
            TaskinQueue = deviceList.Count;
            for (Int32 i = 0; i < deviceList.Count; i++)
            {
                resumeQueue.Enqueue(i);
                var device = deviceList[i];
                device.Status = DeviceStatus.QUEUE;
                device.Info = "In Queue";
            }
            for (Int32 i = 0; i < MaxConcurrentTask; i++)
            {
                Logging.logMessage("Start task: " + i);
                StartRefreshTask();  
            }
        }
    }
}
