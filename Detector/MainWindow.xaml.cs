using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Detector
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : LogWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            deviceResult = new Dictionary<Int32, Task<Boolean>>();
            dispatcherTimer = null;
            Loaded += new RoutedEventHandler(OnMainWindow_Loaded);
        }

        private Dictionary<Int32, Task<Boolean>> deviceResult;
        private DispatcherTimer dispatcherTimer;

        public void OnMainWindow_Loaded(Object sender, RoutedEventArgs e)
        {
            Logging.Initialize(this, logBox);
            Logging.logMessage("Hello");

            grid.DataContext = App.DM.DeviceList;

        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var editDeviceWindow = new AboutWindow();
            editDeviceWindow.ShowDialog();
        }

        private void SaveDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            DB.SaveDataAsync();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            var toRemoved = new List<Int32>();
            foreach (var result in deviceResult)
            {
                var index = result.Key;
                var taskResult = result.Value;
                if (taskResult != null && taskResult.IsCompleted)
                {
                    try
                    {
                        Boolean deviceStatus = taskResult.Result;
                        Logging.logMessage(String.Format("Device {0} status is {1}!", App.DM.DeviceList[index].IP, deviceStatus));
                        App.DM.DeviceList[index].Status = deviceStatus ? DeviceStatus.PASS : DeviceStatus.FAIL;
                        App.DM.DeviceList[index].Info = String.Format("Ping result: {0}", deviceStatus);
                    }
                    catch (Exception ex)
                    {
                        Logging.logMessage(String.Format("Check device {0} with exception:", App.DM.DeviceList[index].IP), ex);
                        App.DM.DeviceList[index].Status = DeviceStatus.FAIL;
                        App.DM.DeviceList[index].Info = ex.Message;
                    }
                    toRemoved.Add(index);
                }
            }
            foreach (var index in toRemoved)
            {
                deviceResult.Remove(index);
                Logging.logMessage(String.Format("Remove result of id {0}!", index));
            }
        }

        private void StartRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(OnTimedEvent);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            for (Int32 i = 0; i < App.DM.DeviceList.Count; i++ )
            {
                var device = App.DM.DeviceList[i];
                if (device.Type == DeviceType.IP)
                {
                    deviceResult[i] = Detect.PingAsync(device.IP);
                }
                else if (device.Type == DeviceType.ADB_IP)
                {
                    deviceResult[i] = Detect.PingRemoteADBAsync(device.IP, device.ADB);
                }
                Logging.logMessage(String.Format("Start to ping device {0} of id {1}", device.IP, i));
                device.Status = DeviceStatus.None;
                device.Info = "In query";
            }
        }

    }
}
