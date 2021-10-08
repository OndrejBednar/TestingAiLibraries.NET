using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestingComputerVision.NET
{
    public partial class Form1 : Form
    {
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        VideoCapture cap;
        List<Mat> TrainedFaces = new List<Mat>();
        List<int> PersonLabels = new List<int>();
        bool EnabledSaveImage = false;
        bool IsTrained = false;
        EigenFaceRecognizer recognizer;
        Regex pattern = new Regex("[.:]");

        public Form1()
        {
            InitializeComponent();
            foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0;
            TrainImagesFromDir();
        }

        private void LoadImageClicked(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var SImg = CvInvoke.Imread(dialog.FileName).ToImage<Bgr, byte>();
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

            //Step 1: getting the capture frame
            Mat m = new Mat();
            cap.Retrieve(m);
            Image<Bgr, byte> img = m.ToImage<Bgr, byte>();



            //Step 2: detecting faces in the image
            DetectFaces(img);

            //showing image in new window
            CvInvoke.Imshow("Frame", img);
            if (CvInvoke.WaitKey(1) == 'q')
            {
                cap.Stop();
                cap.Dispose();
                comboBox1.Invoke(new Action(() => comboBox1.Enabled = true));
                CvInvoke.DestroyWindow("Frame");
            }
        }
        private void DetectFaces(Image<Bgr, byte> img)
        {
            //Convert Bgr image to Gray image
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            //Enhancing the image to get better result
            CvInvoke.EqualizeHist(gray, gray);

            Rectangle[] faces = cascadeClassifier.DetectMultiScale(gray, 1.1, 4);
            if (faces.Length > 0)
            {
                foreach (var face in faces)
                {
                    CvInvoke.Rectangle(img, face, new Bgr(Color.FromArgb(255, 0, 0)).MCvScalar, 2);
                    //img.Draw(face, new Bgr(255, 0, 0), 2);

                    //Step 3: Add person
                    Image<Bgr, byte> resultImage = img.Copy();
                    resultImage.ROI = face;

                    string path = Directory.GetCurrentDirectory() + @"\TrainedFaces";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);


                    //if (EnabledSaveImage)
                    {
                        //we will save 10 images with delay a second for each image
                        Task.Factory.StartNew(() =>
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                //resize the image then save it
                                resultImage.Resize(200, 200, Emgu.CV.CvEnum.Inter.Cubic).Save("./TrainedFaces/pepa " + 
                                    pattern.Replace(DateTime.Now.ToString("G"), "-") + ".jpg");

                                Thread.Sleep(1000);
                            }
                        });
                    }
                    EnabledSaveImage = false;

                    //Step 5: Recognize the face
                    if (IsTrained)
                    {
                        Image<Gray, byte> grayFaceResult = resultImage.Convert<Gray, byte>().Resize(200, 200, Emgu.CV.CvEnum.Inter.Cubic);

                        grayFaceResult._EqualizeHist();
                        var result = recognizer.Predict(grayFaceResult);

                        //found known faces
                        if (result.Label >= 0)
                        {
                            CvInvoke.PutText(img, PersonLabels[result.Label].ToString(), new Point(face.X - 2, face.Y - 2),
                               Emgu.CV.CvEnum.FontFace.HersheyComplex, 1.0, new Bgr(Color.Green).MCvScalar);
                        }
                        //didnt find any known faces
                        else
                        {
                            CvInvoke.PutText(img, "Unknown", new Point(face.X - 2, face.Y - 2),
                               Emgu.CV.CvEnum.FontFace.HersheyComplex, 1.0, new Bgr(Color.Blue).MCvScalar);
                        }
                    }
                }
            }
        }

        //Step 4: Train images
        private bool TrainImagesFromDir()
        {
            int imageCount = 0;
            int Threshold = 7000;
            TrainedFaces.Clear();
            PersonLabels.Clear();
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\TrainedFaces";
                string[] files = Directory.GetFiles(path, "*.jpg", searchOption: SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    Mat trainedImage = new Mat(file,Emgu.CV.CvEnum.ImreadModes.Grayscale);
                    TrainedFaces.Add(trainedImage);
                    pictureBox1.Image = trainedImage.ToBitmap();
                    PersonLabels.Add(imageCount);


                    imageCount++;
                }
                
                recognizer = new EigenFaceRecognizer(imageCount, Threshold);
                recognizer.Train(TrainedFaces.ToArray(), PersonLabels.ToArray());

                IsTrained = true;
                Debug.WriteLine(imageCount);
                Debug.WriteLine(IsTrained);
                return IsTrained;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in training images: " + ex.Message);
                IsTrained = false;
                return IsTrained;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            EnabledSaveImage = true;
        }
    }
}
