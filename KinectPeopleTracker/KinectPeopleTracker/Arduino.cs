using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace KinectPeopleTracker
{
    class Arduino
    {
        public const int DEFAULT_BAUD_RATE = 9600;
        //public const int DEFAULT_BAUD_RATE = 115200;

        private SerialPort sp;

        private bool connected = false;
        public bool IsConnected { get { return connected; } }
        
        public Arduino()
        {
        }

        #region Events

        //public delegate void DistancesChangedHandler(double[] dists);
        //public event DistancesChangedHandler DistancesChanged;
        //public virtual void OnDistancesChanged(double[] dists)
        //{
        //    DistancesChanged(dists);
        //}

        #endregion

        #region Public Functions

        public bool Connect()
        {
            bool success = OpenPort(Properties.Settings.Default.ComPort, DEFAULT_BAUD_RATE);
            if (success) connected = true;
            return success;
        }

        public bool Disconnect()
        {
            bool success = ClosePort();
            connected = false;
            return success;
        }

        public void Send(string text)
        {
            SendData(text);
        }

        #endregion

        #region Private Functions

        private bool OpenPort(string port, int baudRate)
        {
            try
            {
                sp = new SerialPort(port, baudRate);
                if (!sp.IsOpen)
                    sp.Open();
                //sp.DataReceived += new SerialDataReceivedEventHandler(ArduinoDataReceived);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ClosePort()
        {
            try
            {
                Task.Factory.StartNew(() => { sp.Close(); });
                Thread.Sleep(500);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SendData(string data)
        {
            lock (sp)
            {
                try
                {
                    sp.Write(data + "\n");
                }
                catch { }
            }
        }

        #endregion

        #region Event Handlers

        //private void ArduinoDataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    try
        //    {
        //        string data = sp.ReadLine();
        //        while (data.Length == 0 || data[0] != '=')
        //            data = sp.ReadLine();

        //        List<double> dists = new List<double>();
        //        string[] vals = data.Substring(1).Split(',');
        //        foreach(string val in vals)
        //        {
        //            try
        //            {
        //                double dist = double.Parse(val);
        //                dists.Add(dist);
        //            }
        //            catch {}
        //        }
        //        OnDistancesChanged(dists.ToArray());
        //    }
        //    catch { }
        //}

        #endregion
    }
}
