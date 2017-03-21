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
            Logging.logMessage("Task PingRemoteADB: " + asIP);
            var result = false;
            var ifconfig_result = ConnectRemouteADB(asIP, ASPort, sn, "ifconfig lo");
            if (ifconfig_result.IndexOf("oopback") > -1)
            {
                Logging.logMessage("Got ifconfig loopback: ", ifconfig_result);
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

            foreach (var addr in Dns.GetHostEntry(asIP).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = addr;
                    break;
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
            Logging.logMessage("Socket connected to {0}", sender.RemoteEndPoint.ToString());  
  
            // Encode the data string into a byte array.  
            byte[] msg = Encoding.ASCII.GetBytes(String.Format("adb -s {0} shell {1}\n", sn, cmd));
            // Send the data through the socket.
            Logging.logMessage("Send msg:", msg.Length);
            int bytesSent = sender.Send(msg);
            sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
            // Receive the response from the remote device.  
            int bytesRec = sender.Receive(bytes);
            result = Encoding.UTF8.GetString(bytes, 0, bytesRec);
            var displayLen = 10;
            if (result.Length < displayLen)
            {
                displayLen = result.Length;
            }
            Logging.logMessage("Remote ADB response:", result.Substring(0, displayLen));

            // Release the socket.  
            sender.Shutdown(SocketShutdown.Both);  
            sender.Close();  

            return result;
        }

    }
}
