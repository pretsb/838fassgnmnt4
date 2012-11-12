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
        private byte[] colorData;
        private int personCount = 0;
        private Font personCountFont;

        private Arduino arduino;

        private PointF motionVector = new PointF(1, 0);
        private Dictionary<int, List<PointF>> trackedPeople;

        public Form1()
        {
            InitializeComponent();

            trackedPeople = new Dictionary<int, List<PointF>>();
            personCountFont = new Font(Font.FontFamily, 64);

            arduino = new Arduino();
            arduino.Connect();

            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                sensor.SkeletonStream.Enable();
                sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);
                sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);

                depthData = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                colorData = new byte[sensor.ColorStream.FramePixelDataLength];

                sensor.Start();
                sensor.ElevationAngle = 0;
            }
        }

        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            try
            {
                ColorImageFrame frame = e.OpenColorImageFrame();

                if (frame != null)
                {
                    frame.CopyPixelDataTo(colorData);
                    double totalIntensity = 0;
                    for (int i = 0; i < colorData.Length; i += 4)
                    {
                        totalIntensity += (colorData[i] + colorData[i+1] + colorData[i+2]) / 3;
                    }
                    double averageIntensity = totalIntensity / ((double)frame.Width * frame.Height);

                    if (averageIntensity < 50) arduino.Send("move");

                    frame.Dispose();
                }
            }
            catch { }
        }

        internal class PersonCentroid { public float x, y, count; public PersonCentroid(float x, float y, float count) { this.x = x; this.y = y; this.count = count; } }
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

                        Dictionary<int, PersonCentroid> people = new Dictionary<int, PersonCentroid>();

                        // write over the existing image pixel by pixel
                        // also extract tracked person centroids
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

                                    if (player > 0)
                                    {
                                        if (!people.ContainsKey(player))
                                            people[player] = new PersonCentroid(x, y, 1);
                                        else
                                        {
                                            people[player].x += x;
                                            people[player].y += y;
                                            people[player].count++;
                                        }
                                    }

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
                        
                        // draw the centroids
                        Graphics g = Graphics.FromImage(depthImage);
                        foreach (KeyValuePair<int, PersonCentroid> p in people)
                        {
                            g.FillEllipse(Brushes.Black, p.Value.x / p.Value.count - 5, p.Value.y / p.Value.count - 5, 11, 11);
                            g.FillEllipse(Brushes.White, p.Value.x / p.Value.count - 3, p.Value.y / p.Value.count - 3, 7, 7);
                        }

                        // continue tracking all visible (and segmented) people
                        foreach (KeyValuePair<int, PersonCentroid> p in people)
                        {
                            float x = p.Value.x / p.Value.count;
                            float y = p.Value.x / p.Value.count;
                            if (!trackedPeople.ContainsKey(p.Key)) trackedPeople[p.Key] = new List<PointF>();
                            trackedPeople[p.Key].Add(new PointF(x, y));
                        }

                        // check for people who have disappeared and increment/decrement the counter
                        foreach (KeyValuePair<int, List<PointF>> person in trackedPeople)
                        {
                            if (!people.ContainsKey(person.Key))
                            {
                                // calculate path motion and length
                                PointF p0 = person.Value[0];
                                PointF p1 = person.Value[person.Value.Count - 1];
                                float dx = p1.X - p0.X;
                                float dy = p1.Y - p0.Y;
                                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                                
                                // if it matches the appropriate motion pattern, adjust the room count
                                if (dx < 0 && len > 100) personCount--;
                                else if (dx > 0 && len > 100) personCount++;

                                // stop tracking this person
                                trackedPeople.Remove(person.Key);
                            }
                        }

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
                        e.Graphics.DrawString(personCount.ToString(), personCountFont, Brushes.Black, DisplayPanel.Width - e.Graphics.MeasureString(personCount.ToString(), personCountFont).Width - 7, 13);
                        e.Graphics.DrawString(personCount.ToString(), personCountFont, Brushes.White, DisplayPanel.Width - e.Graphics.MeasureString(personCount.ToString(), personCountFont).Width - 10, 10);
                    }
                }
            }
            catch { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensor != null) sensor.Stop();
            if (arduino != null) arduino.Disconnect();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                arduino.Send("move");
            }
        }
    }
}
