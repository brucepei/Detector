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
using System.Windows.Controls.Primitives;

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
            Loaded += new RoutedEventHandler(OnMainWindow_Loaded);
            RepeatRefreshDevicesHandle += new RefreshDevicesDoneHandle(RepeatRefreshDevices);
            RefreshTimeout = 300;
            RefreshRemains = RefreshTimeout;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(OnWaitingRefresh);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }
        public override RichTextBox LogBox
        {
            get { return logBox; }
        }
        public DispatcherTimer dispatcherTimer;
        public Int32 RefreshTimeout;
        public Int32 RefreshRemains;
        public RefreshDevicesDoneHandle RepeatRefreshDevicesHandle;
        public void RepeatRefreshDevices()
        {
            cancelRefreshBtn.IsEnabled = true;
            dispatcherTimer.Start();
        }

        public void OnWaitingRefresh(object sender, EventArgs e)
        {
            remainTextBlock.Text = RefreshRemains.ToString();
            if (--RefreshRemains < 0)
            {
                cancelRefreshBtn.IsEnabled = false;
                RefreshRemains = RefreshTimeout;
                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    Logging.logMessage("Ready to next refresh!");
                    startRefreshBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
            }
        }

        public void OnMainWindow_Loaded(Object sender, RoutedEventArgs e)
        {
            Logging.Initialize(this);
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
            App.DM.SaveDevices();
        }

        private void StartRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            App.DM.RefreshDevices();
            startRefreshBtn.IsEnabled = false;
        }

        private void CancelRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            startRefreshBtn.IsEnabled = true;
            cancelRefreshBtn.IsEnabled = false;
        }
    }
}
