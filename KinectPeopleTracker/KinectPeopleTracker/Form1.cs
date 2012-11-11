using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Kinect;
using System.Drawing.Imaging;

namespace KinectPeopleTracker
{
    public partial class Form1 : Form
    {
        private KinectSensor sensor;
        private Color[] playerColors = { Color.White, Color.Blue, Color.Red, Color.Green, Color.Yellow, Color.Cyan, Color.Magenta };
        private Bitmap depthImage = null;
        private DepthImagePixel[] depthData;

        public Form1()
        {
            InitializeComponent();

            sensor = KinectSensor.KinectSensors[0];
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);
            depthData = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
            sensor.Start();
        }

        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            try
            {
                DepthImageFrame frame = e.OpenDepthImageFrame();

                if (frame != null)
                {
                    frame.CopyDepthImagePixelDataTo(depthData);
                    int maxDepth = frame.MaxDepth;
                    int minDepth = frame.MinDepth;
                    lock (this)
                    {
                        if (depthImage == null) depthImage = new Bitmap(frame.Width, frame.Height);

                        // write over the existing image pixel by pixel
                        BitmapData data = depthImage.LockBits(new Rectangle(0, 0, depthImage.Width, depthImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                        unsafe
                        {
                            byte* p = (byte*)data.Scan0;
                            int i = 0;
                            for (int y = 0; y < data.Height; y++)
                            {
                                for (int x = 0; x < data.Width; x++)
                                {
                                    int depth = depthData[i].Depth;
                                    if (!depthData[i].IsKnownDepth) depth = maxDepth;
                                    int player = depthData[i].PlayerIndex;
                                    Color c = playerColors[player];
                                    float depthPercent = 1.0f - (float)(depth - minDepth) / (float)(maxDepth - minDepth);
                                    *p = (byte)(c.R * depthPercent);
                                    *(p + 1) = (byte)(c.G * depthPercent);
                                    *(p + 2) = (byte)(c.B * depthPercent);
                                    p += 3;
                                    i++;
                                }
                                p += data.Stride - 3 * data.Width;
                            }
                        }
                        depthImage.UnlockBits(data);

                        Invoke(new MethodInvoker(delegate { DisplayPanel.Refresh(); }));
                    }

                    frame.Dispose();
                }
            }
            catch { }
        }

        private void DisplayPanel_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (depthImage != null)
                {
                    lock (this)
                    {
                        e.Graphics.DrawImage(depthImage, 0, 0, depthImage.Width, depthImage.Height);
                    }
                }
            }
            catch { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensor != null) sensor.Stop();
        }
    }
}
