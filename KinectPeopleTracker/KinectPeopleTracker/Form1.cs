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
using System.IO.Ports;

namespace KinectPeopleTracker
{
    public partial class Form1 : Form
    {
        private int colorRate = 200;
        private int depthRate = 30;
        private DateTime lastColor = DateTime.Now;
        private DateTime lastDepth = DateTime.Now;

        private KinectSensor sensor;
        private Color[] playerColors = { Color.White, Color.Blue, Color.Red, Color.Green, Color.Yellow, Color.Cyan, Color.Magenta };
        private Bitmap depthImage = null;
        private DepthImagePixel[] depthData;
        private byte[] colorData;
        private int personCount = 0;
        private Font personCountFont;

        private Arduino arduino;

        private bool positioningExit = false;
        private Dictionary<int, List<Tuple<PointF, int>>> trackedPeople;

        public Form1()
        {
            InitializeComponent();

            trackedPeople = new Dictionary<int, List<Tuple<PointF, int>>>();
            personCountFont = new Font(Font.FontFamily, 64);

            PortChooser.Items.AddRange(SerialPort.GetPortNames());
            PortChooser.SelectedItem = Properties.Settings.Default.ComPort;
            InvertDirectionCheckbox.Checked = Properties.Settings.Default.InvertDirection;
            SizeCheckbox.Checked = Properties.Settings.Default.UseSize;
            InvertSizeCheckbox.Checked = Properties.Settings.Default.InvertSize;

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
                if ((DateTime.Now - lastColor).TotalMilliseconds >= colorRate)
                {
                    ColorImageFrame frame = e.OpenColorImageFrame();

                    if (frame != null)
                    {
                        frame.CopyPixelDataTo(colorData);
                        double totalIntensity = 0;
                        for (int i = 0; i < colorData.Length; i += 4)
                        {
                            totalIntensity += (colorData[i] + colorData[i + 1] + colorData[i + 2]) / 3;
                        }
                        double averageIntensity = totalIntensity / ((double)frame.Width * frame.Height);

                        // lights off
                        if (averageIntensity < 50) arduino.Send("move");

                        frame.Dispose();
                        lastColor = DateTime.Now;
                    }
                }
            }
            catch { }
        }

        internal class PersonCentroid { public float x, y, count; public PersonCentroid(float x, float y, float count) { this.x = x; this.y = y; this.count = count; } }
        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            try
            {
                if ((DateTime.Now - lastDepth).TotalMilliseconds >= depthRate)
                {
                    DepthImageFrame frame = e.OpenDepthImageFrame();

                    if (frame != null)
                    {
                        frame.CopyDepthImagePixelDataTo(depthData);
                        int maxDepth = frame.MaxDepth;
                        int minDepth = frame.MinDepth;

                        if (depthImage == null) depthImage = new Bitmap(frame.Width, frame.Height);

                        lock (this)
                        {
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
                                if (!trackedPeople.ContainsKey(p.Key)) trackedPeople[p.Key] = new List<Tuple<PointF, int>>();
                                trackedPeople[p.Key].Add(new Tuple<PointF, int>(new PointF(x, y), (int)p.Value.count));
                            }

                            Point threshold = Properties.Settings.Default.Threshold;

                            // check for people who have disappeared and increment/decrement the counter
                            foreach (KeyValuePair<int, List<Tuple<PointF, int>>> person in trackedPeople)
                            {
                                if (!people.ContainsKey(person.Key))
                                {
                                    // calculate path motion and length
                                    PointF p0 = person.Value[0].Item1;
                                    PointF p1 = person.Value[person.Value.Count - 1].Item1;
                                    int count1 = person.Value[0].Item2;
                                    int count2 = person.Value[person.Value.Count - 1].Item2;
                                    float dx = p1.X - p0.X;
                                    float dy = p1.Y - p0.Y;
                                    float len = (float)Math.Sqrt(dx * dx + dy * dy);
                                    float maxSize = 0; foreach (Tuple<PointF, int> f in person.Value) if (f.Item2 > maxSize) maxSize = f.Item2;
                                    float sizeRatio = (float)maxSize / (float)count1;
                                    float d1 = (float)Math.Sqrt((p0.X - threshold.X) * (p0.X - threshold.X) + (p0.Y - threshold.Y) * (p0.Y - threshold.Y));
                                    float d2 = (float)Math.Sqrt((p1.X - threshold.X) * (p1.X - threshold.X) + (p1.Y - threshold.Y) * (p1.Y - threshold.Y));

                                    // if it matches the appropriate motion pattern, adjust the room count
                                    bool sufficientMotion = len > 100;
                                    bool sufficientSizeChange = (Properties.Settings.Default.UseSize && (sizeRatio > 1.25 || sizeRatio < 0.75));
                                    bool sizeIncrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && sizeRatio > 1) || (Properties.Settings.Default.InvertSize && sizeRatio < 1));
                                    bool sizeDecrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && sizeRatio < 1) || (Properties.Settings.Default.InvertSize && sizeRatio > 1));
                                    bool positionIncrement = (!Properties.Settings.Default.InvertDirection && d1 < d2) || (Properties.Settings.Default.InvertDirection && d2 < d1);
                                    bool positionDecrement = (!Properties.Settings.Default.InvertDirection && d2 < d1) || (Properties.Settings.Default.InvertDirection && d1 < d2);
                                    bool increment = (sufficientSizeChange && sizeIncrement) || (sufficientMotion && positionIncrement);
                                    bool decrement = (sufficientSizeChange && sizeDecrement) || (sufficientMotion && positionDecrement);

                                    if (decrement) personCount--;
                                    if (increment) personCount++;

                                    // stop tracking this person
                                    trackedPeople.Remove(person.Key);
                                }
                            }

                            Invoke(new MethodInvoker(delegate { DisplayPanel.Refresh(); }));
                        }

                        frame.Dispose();
                        lastDepth = DateTime.Now;
                    }
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

                    Point threshold = Properties.Settings.Default.Threshold;
                    if (positioningExit)
                    {
                        e.Graphics.FillEllipse(Brushes.Blue, threshold.X - 10, threshold.Y - 10, 21, 21);
                        e.Graphics.FillEllipse(Brushes.Black, threshold.X - 7, threshold.Y - 7, 15, 15);
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
            //if (e.KeyCode == Keys.Space)
            //{
            //    arduino.Send("move");
            //}
        }

        private void ThresholdButton_Click(object sender, EventArgs e)
        {
            positioningExit = true;
            ThresholdButton.Enabled = false;
            DisplayPanel.Refresh();
        }

        private void DisplayPanel_MouseClick(object sender, MouseEventArgs e)
        {
            Properties.Settings.Default.Threshold = new Point(e.X, e.Y);
            positioningExit = false;
            ThresholdButton.Enabled = true;
            DisplayPanel.Refresh();
        }

        private void SizeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.UseSize = SizeCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void InvertDirectionCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.InvertDirection = InvertDirectionCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void InvertSizeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.InvertSize = InvertSizeCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void ArmCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableArm = ArmCheckbox.Checked;
            Properties.Settings.Default.Save();
        }

        private void ResetCounterButton_Click(object sender, EventArgs e)
        {
            personCount = 0;
            DisplayPanel.Refresh();
        }

        private void PortChooser_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ComPort = (string)PortChooser.SelectedItem;
            Properties.Settings.Default.Save();
            if (arduino != null && arduino.IsConnected)
            {
                arduino.Disconnect();
                arduino.Connect();
            }
        }      
    }
}
