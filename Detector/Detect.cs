using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;  

namespace Detector
{
    public enum ErrorCode
    {
        NoError = 0x0000,
        PingFailError,
        PingExceptionError,
        RemoteADBFailError,
        RemoteADBExceptionError,
    }

    class Detect
    {
        public static Int32 ASPort = 16789;

        public static Task<Boolean> PingAsync(String targetIp)
        {
            var t = new Task<Boolean> (ip => Ping((String)ip, 3000), targetIp);
            t.Start();
            return t;
        }

        public static Task<Boolean> PingAsync(String targetIp, Device device)
        {
            var t = PingAsync(targetIp);
            t.ContinueWith(task =>
            {
                if (task.Result)
                {
                    device.UpdateStatus(ErrorCode.NoError, "Ping ok");
                }
                else
                {
                    device.UpdateStatus(ErrorCode.PingFailError, "Ping failed");
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            t.ContinueWith(task => device.UpdateStatus(ErrorCode.PingExceptionError, task.Exception.GetOriginalMessage()), TaskContinuationOptions.OnlyOnFaulted);
            return t;
        }

        public static Boolean Ping(String ip, Int32 timeout)
        {
            Logging.logMessage("Task Ping: " + ip);
            bool online = false;
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(ip, timeout);
            if (pingReply.Status == IPStatus.Success)
            {
                online = true;
                Logging.logMessage(String.Format("IP {0} is online!", ip));
            }
            else
            {
                Logging.logMessage(String.Format("IP {0} is offline!", ip));
            }
            return online;
        }

        public struct RemoteADB
        {
            public RemoteADB(String asIP, String sn)
            {
                IP = asIP;
                SerialNumber = sn;
            }
            public String IP;
            public String SerialNumber;
        }

        public static Task<Boolean> PingRemoteADBAsync(String asIP, String sn)
        {
            var t = new Task<Boolean>(ra => PingRemouteADB(((RemoteADB)ra).IP, ((RemoteADB)ra).SerialNumber), new RemoteADB(asIP, sn));
            t.Start();
            return t;
        }

        public static Task<Boolean> PingRemoteADBAsync(String asIP, String sn, Device device)
        {
            var t = PingRemoteADBAsync(asIP, sn);
            t.ContinueWith(task =>
            {
                if (task.Result)
                {
                    device.UpdateStatus(ErrorCode.NoError, "Ping remote ADB ok");
                }
                else
                {
                    device.UpdateStatus(ErrorCode.RemoteADBFailError, "Ping remote ADB failed");
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            t.ContinueWith(task => device.UpdateStatus(ErrorCode.RemoteADBExceptionError, task.Exception.GetOriginalMessage()), TaskContinuationOptions.OnlyOnFaulted);
            return t;
        }

        public static Boolean PingRemouteADB(String asIP, String sn)
        {
            Logging.logMessage(String.Format("{0}:{1} Task PingRemoteADB", asIP, sn));
            var result = false;
            var ifconfig_result = ConnectRemouteADB(asIP, ASPort, sn, "ifconfig lo");
            if (ifconfig_result.IndexOf("oopback") > -1)
            {
                var displayLen = 50;
                if (ifconfig_result.Length < displayLen)
                {
                    displayLen = ifconfig_result.Length;
                }
                Logging.logMessage(String.Format("{0}:{1} Got ifconfig loopback: {2}", asIP, sn, ifconfig_result.Substring(0, displayLen)));
                result = true;
            }
            return result;
        }

        public static String ConnectRemouteADB(String asIP, Int32 asPort, String sn, String cmd)
        {
            String result = String.Empty;
            byte[] bytes = new byte[4096];
            // Connect to a remote device
            IPAddress ipAddress = null;
            if (!IPAddress.TryParse(asIP, out ipAddress))
            {//Dns.GetHostEntry may return wrong IP address if some threads have queried a while ago
                foreach (var addr in Dns.GetHostEntry(asIP).AddressList)
                {
                    Logging.logMessage(String.Format("{0}:{1} addr list:[{2}]", asIP, sn, addr.ToString()));
                }
                foreach (var addr in Dns.GetHostEntry(asIP).AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = addr;
                        Logging.logMessage(String.Format("{0}:{1} resolve IPv4={2}", asIP, sn, ipAddress.ToString()));
                        break;
                    }
                }
            }
            if (ipAddress == null)
            {
                Logging.logMessage("Cannot resolve ip:", asIP);
                return result;
            }
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, asPort);
            // Create a TCP/IP  socket.  
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );  
  
            // Connect the socket to the remote endpoint. Catch any errors.  
            sender.Connect(remoteEP);
            Logging.logMessage(String.Format("{0}:{1} Socket connected to {2}", asIP, sn, sender.RemoteEndPoint.ToString()));  
  
            // Encode the data string into a byte array.
            String command = String.Format("adb -s {0} shell {1}\n", sn, cmd);
            byte[] msg = Encoding.ASCII.GetBytes(command);
            // Send the data through the socket.
            Logging.logMessage(String.Format("Send as command:[{0}]", command));
            int bytesSent = sender.Send(msg);
            sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
            // Receive the response from the remote device.  
            int bytesRec = sender.Receive(bytes);
            result = Encoding.UTF8.GetString(bytes, 0, bytesRec);
            var displayLen = 100;
            if (result.Length < displayLen)
            {
                displayLen = result.Length;
            }
            Logging.logMessage(String.Format("{0}:{1} ping Remote ADB response:{2}", asIP, sn, result.Substring(0, displayLen)));

            // Release the socket.  
            sender.Shutdown(SocketShutdown.Both);  
            sender.Close();  

            return result;
        }

    }
}
