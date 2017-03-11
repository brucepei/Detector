using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.Collections.ObjectModel;
using System.IO;

namespace Detector
{
    public enum ChangedType
    {
        APPEND,
        DELETE,
        MODIFY,
    }
    public struct ChangedOp
    {
        public ChangedOp(ChangedType t, Device d)
        {
            type = t;
            device = d;
        }

        public ChangedType type;
        public Device device;
    }

    public static class DB
    {
        public static Boolean SaveBusy = false;
        private static readonly object _locker = new object();
        private static List<ChangedOp> _changedOp= new List<ChangedOp>();
        public static void ChangeDB(ChangedType type, Device device)
        {
            if( _changedOp.Count > 0 )
            {
                var lastOp = _changedOp[_changedOp.Count-1];
                if (type == lastOp.type && device == lastOp.device)
                {
                    return;
                }
            }
            _changedOp.Add(new ChangedOp(type, device));
        }
        private static String _dbFileName = "detector_db.json";

        public static ObservableCollection<Device> LoadDevices()
        {
            string toDes = String.Empty;
            ObservableCollection<Device> deviceList = null;
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(_dbFileName);
                toDes = file.ReadToEnd();
                file.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to read db file: " + _dbFileName + " ex: " + ex.GetOriginalException().Message);
            }
            if (toDes.Length > 0)
            {
                var ms = new MemoryStream(Encoding.Unicode.GetBytes(toDes));
                DataContractJsonSerializer deseralizer = new DataContractJsonSerializer(typeof(ObservableCollection<Device>));
                try
                {
                    deviceList = (ObservableCollection<Device>)deseralizer.ReadObject(ms);
                    Console.WriteLine("Count=" + deviceList.Count);
                    Console.WriteLine("Name0=" + deviceList[0].Name);
                    Console.WriteLine("IP0=" + deviceList[0].IP);
                    Console.WriteLine("Info0=" + deviceList[0].Info);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Failed to transfer db to device list!" + " ex: " + ex.GetOriginalException().Message);
                }
            }
            return deviceList;
        }

        public static Boolean SaveData()
        {
            var result = false;
            if (SaveBusy)
            {
                Logging.logMessage("Save DB is busy, ignore sync save db request!");
            }
            else
            {
                foreach (var op in _changedOp)
                {
                    Logging.logMessage(String.Format("Device {0}({1}): {2}!", op.device.Id, op.device.Name, op.type.ToString()));
                }
                lock (_locker) SaveBusy = true;
                Logging.logMessage("Save db starts...");
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(ObservableCollection<Device>));
                MemoryStream msObj = new MemoryStream();
                js.WriteObject(msObj, App.DM.DeviceList);
                msObj.Position = 0;
                StreamReader sr = new StreamReader(msObj, Encoding.UTF8);
                string json = sr.ReadToEnd();
                sr.Close();
                msObj.Close();
                try
                {
                    System.IO.StreamWriter file = new System.IO.StreamWriter(_dbFileName, false);
                    file.Write(json);
                    file.Close();
                }
                catch (Exception ex)
                {
                    Logging.logMessage("Failed to write db file: " + _dbFileName + " ex: " + ex.GetOriginalException().Message);
                }
                Logging.logMessage("Save db done!", json);
                lock (_locker) SaveBusy = false;
                _changedOp.Clear();
            }
            return result;
        }

        public static Task<Boolean> SaveDataAsync()
        {
            Task<Boolean> task = null;
            if (SaveBusy)
            {
                Logging.logMessage("Save DB is busy, ignore async save db request!");
            }
            else
            {
                task = new Task<Boolean>(() => SaveData());
                task.Start();
            }
            return task;
        }
    }
}
