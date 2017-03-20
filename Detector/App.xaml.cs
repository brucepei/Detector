using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Detector
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            Logging.Initialize(false, false);
            //Logging.Initialize(true, true);
            //Logging.Initialize(true, false);
            dm = new DeviceManage();
        }

        private static DeviceManage dm;
        public static DeviceManage DM
        {
            get { return dm; }
        }
        
    }
}
