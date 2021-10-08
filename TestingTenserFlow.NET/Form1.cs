using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestingComputerVision.NET
{
    public partial class Form1 : Form
    {
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
        VideoCapture cap;
        public Form1()
        {
            InitializeComponent();
            foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0;
        }

        private void LoadImageClicked(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var SImg = CvInvoke.Imread(dialog.FileName).ToImage<Bgr,byte>();
            DetectFaces(SImg);
            //CvInvoke.Resize(SImg, SImg, Size, interpolation: Emgu.CV.CvEnum.Inter.Cubic);
            CvInvoke.Imshow("Image", SImg);
            CvInvoke.WaitKey();
            CvInvoke.DestroyAllWindows();
        }
        private void StartRecordingClicked(object sender, EventArgs e)
        {
            cap = new VideoCapture(comboBox1.SelectedIndex);
            comboBox1.Enabled = false;
            if (!cap.IsOpened)
            {
                return;
            }
            cap.ImageGrabbed += FrameCaptured;
            cap.Start();
        }

        private void FrameCaptured(object sender, EventArgs e)
        {
            try
            {
                Mat m = new Mat();
                cap.Retrieve(m);
                Image<Bgr, byte> img = m.ToImage<Bgr, byte>();
                DetectFaces(img);
                CvInvoke.Imshow("Frame", img);
                if (CvInvoke.WaitKey(1) == 'q')
                {
                    cap.Stop();
                    cap.Dispose();
                    comboBox1.Invoke(new Action(() => comboBox1.Enabled = true));
                    CvInvoke.DestroyWindow("Frame");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void DetectFaces(Image<Bgr, byte> img)
        {
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            var faces = cascadeClassifier.DetectMultiScale(gray, 1.3, 5);
            foreach (var face in faces)
            {
                img.Draw(face, new Bgr(255, 0, 0), 2);
            }
        }

    }
}
