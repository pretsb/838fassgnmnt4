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

using AForge.Video.FFMPEG;

namespace KinectPeopleTracker
{
    public partial class Form1 : Form
    {
        private int colorRate = 200;
        private int depthRate = 30;
        private DateTime lastColor = DateTime.Now;
        private DateTime lastDepth = DateTime.Now;

        private enum WaveState { Vertical, Left, Right, Other };
        private WaveState wavingState = WaveState.Other;
        private int waveTimeout = 1000;
        int waveCounter = 0;
        private DateTime lastWaveState = DateTime.Now;

        private KinectSensor sensor;
        private Color[] playerColors = { Color.White, Color.Blue, Color.Red, Color.Green, Color.Yellow, Color.Cyan, Color.Magenta };
        private Bitmap depthImage = null, colorImage = null;
        private Bitmap videoFrame = null;
        private DepthImagePixel[] depthData;
        private Skeleton[] skeletons = new Skeleton[0];
        private byte[] colorData;
        private int personCount = 0;
        private Font personCountFont;

        private Arduino arduino;

        private bool positioningExit = false;
        private Dictionary<int, List<Tuple<PointF, float>>> trackedPeople;

        private VideoFileWriter videoOut = null;
        private bool recording = false;

        public Form1()
        {
            InitializeComponent();

            trackedPeople = new Dictionary<int, List<Tuple<PointF, float>>>();
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
                sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);

                depthData = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                colorData = new byte[sensor.ColorStream.FramePixelDataLength];

                sensor.Start();
                sensor.ElevationAngle = 0;
            }
        }

        void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            try
            {
                SkeletonFrame frame = e.OpenSkeletonFrame();

                if (frame != null)
                {
                    if (this.skeletons.Length != frame.SkeletonArrayLength)
                    {
                        this.skeletons = new Skeleton[frame.SkeletonArrayLength];
                    }
                    frame.CopySkeletonDataTo(this.skeletons);

                    // Assume no nearest skeleton and that the nearest skeleton is a long way away.
                    double nearestDistance2 = double.MaxValue;
                    int skeletonIndex = -1;

                    // Look through the skeletons.
                    int i = 0;
                    foreach (var skeleton in this.skeletons)
                    {
                        // Only consider tracked skeletons.
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // Find the distance squared.
                            var distance2 = (skeleton.Position.X * skeleton.Position.X) +
                                (skeleton.Position.Y * skeleton.Position.Y) +
                                (skeleton.Position.Z * skeleton.Position.Z);

                            // Is the new distance squared closer than the nearest so far?
                            if (distance2 < nearestDistance2)
                            {
                                // Use the new values.
                                nearestDistance2 = distance2;
                                skeletonIndex = i;
                            }
                        }
                        i++;
                    }

                    // process skeletons, look for gesture
                    if (skeletonIndex >= 0 && skeletons[skeletonIndex].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        SkeletonPoint elbow = skeletons[skeletonIndex].Joints[JointType.ElbowRight].Position;
                        SkeletonPoint wrist = skeletons[skeletonIndex].Joints[JointType.WristRight].Position;
                        float dx = wrist.X - elbow.X;
                        float dy = wrist.Y - elbow.Y;
                        double angle = Math.Atan2(dy, dx);
                        WaveState formerState = wavingState;
                        if (angle >= 0 && angle < Math.PI / 3.0) wavingState = WaveState.Right;
                        else if (angle >= Math.PI / 3.0 && angle < 2 * Math.PI / 3.0) wavingState = WaveState.Vertical;
                        else if (angle >= 2 * Math.PI / 3.0 && angle < Math.PI) wavingState = WaveState.Left;
                        else wavingState = WaveState.Other;

                        if (waveCounter > 0 && (DateTime.Now - lastWaveState).TotalMilliseconds > waveTimeout)
                            waveCounter = 0;
                        else
                        {
                            if (formerState != wavingState && formerState != WaveState.Other && wavingState != WaveState.Other)
                            {
                                waveCounter++;
                                lastWaveState = DateTime.Now;
                            }
                        }

                        if (waveCounter >= 3) { arduino.Send("move"); waveCounter = 0; }
                    }

                    frame.Dispose();
                }
            }
            catch { }
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

                        if (colorImage == null) colorImage = new Bitmap(frame.Width, frame.Height);

                        lock (this)
                        {
                            BitmapData data = colorImage.LockBits(new Rectangle(0, 0, colorImage.Width, colorImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                            unsafe
                            {
                                byte* p = (byte*)data.Scan0;
                                int i = 0;
                                for (int y = 0; y < data.Height; y++)
                                {
                                    for (int x = 0; x < data.Width; x++)
                                    {
                                        byte r = colorData[i];
                                        byte g = colorData[i + 1];
                                        byte b = colorData[i + 2];
                                        totalIntensity += (r + g + b) / 3;
                                        byte a = colorData[i + 3];
                                        *p = r;
                                        *(p + 1) = g;
                                        *(p + 2) = b;
                                        p += 3;
                                        i += 4;
                                    }
                                    p += data.Stride - 3 * data.Width;
                                }
                            }
                            colorImage.UnlockBits(data);
                        }
                        double averageIntensity = totalIntensity / ((double)frame.Width * frame.Height);

                        // lights off
                        if (Properties.Settings.Default.EnableArm && averageIntensity < 50)// && personCount > 04) 
                            arduino.Send("move");

                        frame.Dispose();
                        lastColor = DateTime.Now;
                    }
                }
            }
            catch { }
        }

        internal class PersonCentroid { public float x, y, count, depth; public PersonCentroid(float x, float y, float count, float depth) { this.x = x; this.y = y; this.count = count; this.depth = depth; } }
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
                                                people[player] = new PersonCentroid(x, y, 1, depth);
                                            else
                                            {
                                                people[player].x += x;
                                                people[player].y += y;
                                                people[player].count++;
                                                people[player].depth += depth;
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
                                float y = p.Value.y / p.Value.count;
                                float d = p.Value.depth / p.Value.count;
                                if (!trackedPeople.ContainsKey(p.Key)) trackedPeople[p.Key] = new List<Tuple<PointF, float>>();
                                trackedPeople[p.Key].Add(new Tuple<PointF, float>(new PointF(x, y), d));
                            }

                            Point threshold = Properties.Settings.Default.Threshold;

                            // check for people who have disappeared and increment/decrement the counter
                            List<int> toRemove = new List<int>();
                            foreach (KeyValuePair<int, List<Tuple<PointF, float>>> person in trackedPeople)
                            {
                                if (!people.ContainsKey(person.Key))
                                {
                                    // calculate path motion and length
                                    PointF p0 = person.Value[0].Item1;
                                    PointF p1 = person.Value[person.Value.Count - 1].Item1;
                                    float depth1 = person.Value[0].Item2;
                                    float depth2 = person.Value[person.Value.Count - 1].Item2;
                                    float dx = p1.X - p0.X;
                                    float dy = p1.Y - p0.Y;
                                    float len = (float)Math.Sqrt(dx * dx + dy * dy);
                                    //float maxSize = 0; foreach (Tuple<PointF, int> f in person.Value) if (f.Item2 > maxSize) maxSize = f.Item2;
                                    //float sizeRatio = (float)maxSize / (float)count1;
                                    float depthLen = Math.Abs(depth2 - depth1);
                                    float d1 = (float)Math.Sqrt((p0.X - threshold.X) * (p0.X - threshold.X) + (p0.Y - threshold.Y) * (p0.Y - threshold.Y));
                                    float d2 = (float)Math.Sqrt((p1.X - threshold.X) * (p1.X - threshold.X) + (p1.Y - threshold.Y) * (p1.Y - threshold.Y));

                                    // if it matches the appropriate motion pattern, adjust the room count
                                    bool sufficientMotion = len > 100;
                                    //bool sufficientSizeChange = (Properties.Settings.Default.UseSize && (sizeRatio > 1.25 || sizeRatio < 0.75));
                                    //bool sizeIncrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && sizeRatio > 1) || (Properties.Settings.Default.InvertSize && sizeRatio < 1));
                                    //bool sizeDecrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && sizeRatio < 1) || (Properties.Settings.Default.InvertSize && sizeRatio > 1));
                                    bool sufficientDepthChange = (Properties.Settings.Default.UseSize && depthLen > 500);
                                    bool depthIncrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && depth2 < depth1) || (Properties.Settings.Default.InvertSize && depth1 < depth2));
                                    bool depthDecrement = Properties.Settings.Default.UseSize && ((!Properties.Settings.Default.InvertSize && depth1 < depth2) || (Properties.Settings.Default.InvertSize && depth2 < depth1));
                                    bool positionIncrement = (!Properties.Settings.Default.InvertDirection && d1 < d2) || (Properties.Settings.Default.InvertDirection && d2 < d1);
                                    bool positionDecrement = (!Properties.Settings.Default.InvertDirection && d2 < d1) || (Properties.Settings.Default.InvertDirection && d1 < d2);
                                    bool increment = (sufficientDepthChange && depthIncrement) || (sufficientMotion && positionIncrement);
                                    bool decrement = (sufficientDepthChange && depthDecrement) || (sufficientMotion && positionDecrement);

                                    if (decrement) personCount--;
                                    if (increment) personCount++;

                                    // stop tracking this person
                                    toRemove.Add(person.Key);
                                }
                            }

                            foreach (int index in toRemove)
                                trackedPeople.Remove(index);

                            if (recording && videoOut != null)
                            {
                                if (videoFrame == null) videoFrame = new Bitmap(1280, 720);
                                g = Graphics.FromImage(videoFrame);
                                g.DrawImage(colorImage, 0, 0, colorImage.Width, colorImage.Height);
                                g.DrawImage(depthImage, colorImage.Width, 0, depthImage.Width, depthImage.Height);
                                g.FillRectangle(Brushes.Black, 0, colorImage.Height, videoFrame.Width, videoFrame.Height);
                                g.DrawString(personCount.ToString(), personCountFont, Brushes.White, colorImage.Width - g.MeasureString(personCount.ToString(), personCountFont).Width / 2, depthImage.Height + 20);
                                videoOut.WriteVideoFrame(videoFrame);
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
            if (positioningExit)
            {
                Properties.Settings.Default.Threshold = new Point(e.X, e.Y);
                Properties.Settings.Default.Save();
                positioningExit = false;
                ThresholdButton.Enabled = true;
                DisplayPanel.Refresh();
            }
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

        private void RecordVideoButton_Click(object sender, EventArgs e)
        {
            if (!recording)
            {
                videoOut = new AForge.Video.FFMPEG.VideoFileWriter();
                videoOut.Open("test.avi", 1280, 720, 10, AForge.Video.FFMPEG.VideoCodec.MSMPEG4v3);
                recording = true;
                RecordVideoButton.Text = "Stop Recording";
            }
            else
            {
                recording = false;
                videoOut.Close();
                videoOut = null;
                RecordVideoButton.Text = "Record Video";
            }
            
            //AVIWriter aviOut = new AVIWriter();
            //aviOut.FrameRate = (int)30;
            //aviOut.Open("test.avi", 640, 480);
        }

        private void ManualWaveButton_Click(object sender, EventArgs e)
        {
            arduino.Send("move");
        }      
    }
}
