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
        private static DeviceManage dm = new DeviceManage();
        public static DeviceManage DM
        {
            get { return dm; }
        }
    }
}
