using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestingComputerVision.NET
{
    public partial class Form1 : Form
    {
        #region variables
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        static readonly int numberOfPics = 5;
        private int FaceIndex { get; set; }
        VideoCapture cap;
        List<Mat> TrainedFaces = new List<Mat>();
        List<int> PersonLabels = new List<int>();
        bool EnabledSaveImage = false, DetectFacesEnabled = false;
        bool IsTrained = false;
        EigenFaceRecognizer recognizer;
        Regex pattern = new Regex("[.:]");
        Dictionary<int[], string> LabelNameDictionary = new Dictionary<int[], string>();
        #endregion
        public Form1()
        {
            InitializeComponent();
            foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0;
            TrainImagesFromDir();
            FaceIndex = PersonLabels.Count - 1;
        }

        private void LoadImageClicked(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var SImg = CvInvoke.Imread(dialog.FileName).ToImage<Bgr, byte>();
            DetectFaces(SImg, false);
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

            //Step 1: getting the captured frame
            Mat m = new Mat();
            cap.Retrieve(m);
            Image<Bgr, byte> img = m.ToImage<Bgr, byte>();



            //Step 2: detecting faces in the image
            if (DetectFacesEnabled) DetectFaces(img, true);

            //showing image in new window
            CvInvoke.Imshow("Frame", img);
            //pictureBox1.Image = img.ToBitmap();
            if (CvInvoke.WaitKey(1) == 'q')
            {
                cap.Stop();
                cap.Dispose();
                comboBox1.Invoke(new Action(() => comboBox1.Enabled = true));
                CvInvoke.DestroyWindow("Frame");
            }
        }
        private void DetectFaces(Image<Bgr, byte> FrontImage, bool video)
        {
            Image<Bgr, byte> BackgroundImage = FrontImage.Copy();

            //Convert Bgr image to Gray image
            Mat gray = new Mat();
            CvInvoke.CvtColor(FrontImage, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            //Enhancing the image to get better result
            CvInvoke.EqualizeHist(gray, gray);

            Rectangle[] faces = cascadeClassifier.DetectMultiScale(gray, 1.05, 7);
            if (faces.Length > 0)
            {
                foreach (var face in faces)
                {

                    //img.Draw(face, new Bgr(255, 0, 0), 2);

                    BackgroundImage.ROI = face;

                    if (EnabledSaveImage)
                    {
                        IsTrained = false;
                        //Step 3: Add person

                        string path = Directory.GetCurrentDirectory() + @"\TrainedFaces";
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        if (video)
                        {
                            SaveFacesVideo(BackgroundImage);
                        }
                        else
                        {
                            SaveFacesPicture(BackgroundImage);
                        }

                    }


                    //Step 5: Recognize the face
                    if (IsTrained)
                    {
                        Image<Gray, byte> grayFaceResult = BackgroundImage.Convert<Gray, byte>().Resize(200, 200, Emgu.CV.CvEnum.Inter.Cubic);
                        pictureBox1.Image = grayFaceResult.ToBitmap();


                        var result = recognizer.Predict(grayFaceResult);

                        //found known faces
                        if (result.Label > -1)
                        {
                            pictureBox2.Image = TrainedFaces[result.Label].ToBitmap();
                            CvInvoke.Rectangle(FrontImage, face, new MCvScalar(0, 255, 0), 2);
                            CvInvoke.PutText(FrontImage, LabelNameDictionary.Where(s => s.Key.Contains(result.Label)).First().Value.ToString(), new Point(face.X - 2, face.Y - 3),
                               Emgu.CV.CvEnum.FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                        }
                        //didnt find any known faces
                        else
                        {
                            CvInvoke.Rectangle(FrontImage, face, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.PutText(FrontImage, "Unknown", new Point(face.X - 2, face.Y - 3),
                               Emgu.CV.CvEnum.FontFace.HersheyComplex, 1.0, new Bgr(Color.Red).MCvScalar);
                        }
                    }
                    else
                    {
                        CvInvoke.Rectangle(FrontImage, face, new MCvScalar(0, 0, 255), 2);
                    }
                }
                if (EnabledSaveImage)
                {
                    EnabledSaveImage = false;
                }
            }
        }
        private void SaveFacesVideo(Image<Bgr, byte> resultImage)
        {
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < numberOfPics; i++)
                {
                    //we will save x images with delay a second for each image
                    //resize the image then save it
                    resultImage.Copy().Resize(200, 200, Emgu.CV.CvEnum.Inter.Cubic).Save("./TrainedFaces/person_" + FaceIndex + " n" + i + ".jpg");

                    Thread.Sleep(1000);
                }
            });
            FaceIndex++;
        }
        private void SaveFacesPicture(Image<Bgr, byte> resultImage)
        {
            //its pointless to take more than 1 image from the picture since its static
            //resize the image then save it
            resultImage.Copy().Resize(200, 200, Emgu.CV.CvEnum.Inter.Cubic).Save("./TrainedFaces/person_" + FaceIndex + ".jpg");
            FaceIndex++;
        }

        //Step 4: Train images  
        private bool TrainImagesFromDir()
        {
            int imageCount = 0;
            int Threshold = 7000;
            TrainedFaces.Clear();
            PersonLabels.Clear();
            LabelNameDictionary.Clear();
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\TrainedFaces";
                string[] files = Directory.GetFiles(path, "*.jpg", searchOption: SearchOption.TopDirectoryOnly);

                List<int> multipleIds = new List<int>();
                foreach (var file in files)
                {
                    Mat trainedImage = new Mat(file, Emgu.CV.CvEnum.ImreadModes.Grayscale);
                    TrainedFaces.Add(trainedImage);
                    PersonLabels.Add(imageCount);
                    string filename = file.Split('\\').Last();
                    if (filename.Split(' ').Length == 2 && filename.Split(' ')[1].Contains("n"))
                    {
                        multipleIds.Add(imageCount);
                        if (multipleIds.Count == numberOfPics)
                        {
                            LabelNameDictionary.Add(multipleIds.ToArray(), filename.Split(' ')[0]);
                            multipleIds.Clear();
                        }
                    }
                    else
                    {
                        LabelNameDictionary.Add(new int[] { imageCount }, filename.Split('.')[0]);
                    }


                    imageCount++;
                }

                int[] labels = new int[PersonLabels.Count];
                PersonLabels.CopyTo(labels, 0);

                recognizer = new EigenFaceRecognizer(imageCount, Threshold);
                recognizer.Train(TrainedFaces.ToArray(), labels);

                IsTrained = true;
                Debug.WriteLine(imageCount);
                Debug.WriteLine(IsTrained);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in training images: " + ex.Message);
                IsTrained = false;
            }
            return IsTrained;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DetectFacesEnabled = !DetectFacesEnabled;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            TrainImagesFromDir();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            EnabledSaveImage = !EnabledSaveImage;
        }
    }
}
