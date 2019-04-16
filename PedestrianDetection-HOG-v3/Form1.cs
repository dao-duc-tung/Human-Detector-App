// #define ShowResponseWindow
// #define DisplayMaxResults

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System.Threading;


namespace PedestrianDetection_HOG_v3
{
    public partial class Form1 : Form
    {
        bool blnFirstTimeInResizeEvent = true;
        int intOrigFormWidth = 0;
        int intOrigFormHeight = 0;
        int intOrigImageBoxWidth = 0;
        int intOrigImageBoxHeight = 0;

        Thread tProcess, tProcess1, tProcess2, tProcess9;
        bool isProcessing = false;

        double[] tan = new double[5];
        float[] svm = HOGDescriptor.GetDefaultPeopleDetector();

        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            intOrigFormWidth = this.Width;
            intOrigFormHeight = this.Height;
            intOrigImageBoxHeight = ibImage.Height;
            intOrigImageBoxWidth = ibImage.Width;

            tan[0] = 0;                 //0 degree
            tan[1] = 1.0 / 4 + 1.0 / 8;     //20 degree
            tan[2] = 1.0 / 2 + 1.0 / 4 + 1.0 / 16;    //40 degree
            tan[3] = 1.0 + 1.0 / 2 + 1.0 / 4;         //60 degree
            tan[4] = 5.0 + 1.0 / 2 + 1.0 / 8 + 1.0 / 32;    //80 degree
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (blnFirstTimeInResizeEvent == true)
            {
                blnFirstTimeInResizeEvent = false;
            }
            else
            {
                ibImage.Width = this.Width - (intOrigFormWidth - intOrigImageBoxWidth);
                ibImage.Height = this.Height - (intOrigFormHeight - intOrigImageBoxHeight);
            }

        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            ofdFile.InitialDirectory = @"C:\Users\tungd\Desktop\temp\INRIAPerson\Test\pos";
            DialogResult drDialogResult = ofdFile.ShowDialog();

            if (drDialogResult == DialogResult.OK || drDialogResult == DialogResult.Yes)
            {
                txtFile.Text = ofdFile.FileName;
                btnProcess_Click(sender, e);
            }
        }

        private void txtFile_TextChanged(object sender, EventArgs e)
        {
            btnProcess_Click(sender, e);
        }

        void ProcessImageAndUpdateGUI()
        {
            Image<Bgr, Byte> imgImage = new Image<Bgr, byte>(txtFile.Text);
            HOGDescriptor hogd = new HOGDescriptor();
            MCvObjectDetection[] rectPedestrians;

            this.Text = "processing, please wait . . .";
            ibImage.Image = null;
            Application.DoEvents();

            hogd.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
            rectPedestrians = hogd.DetectMultiScale(imgImage);
            //rectPedestrians = hogd.DetectMultiScale(imgImage, 0, default(Size), default(Size), 1.05, 0.2, false);

            for (int i = 0; i < rectPedestrians.Length; i++)
            {
                imgImage.Draw(rectPedestrians[i].Rect, new Bgr(Color.GreenYellow), 1);
            }

            ibImage.Image = imgImage.ToBitmap();

            this.Text = "done processing, choose another image if desired";

            //float[] descriptor_vector = hogd.Compute(imgImage);
            //for (int k = 0; k < 100; k++)
            //{
            //    txt1.AppendText(descriptor_vector[k].ToString() + "\n");
            //}
            isProcessing = false;
        }

        void GenerateScaledImage_v3(Mat[] scaledImage, float[] scale_factor, float scale)
        {
            scale_factor[0] = 1;
            Mat down2x = new Mat(scaledImage[0].Height / 2, scaledImage[0].Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(scaledImage[0], down2x);
            scaledImage[4] = down2x.Clone();//[4]
            scale_factor[4] = 2;
            Mat down4x = new Mat(down2x.Height / 2, down2x.Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(down2x, down4x);
            scaledImage[8] = down4x.Clone();//[8]
            scale_factor[8] = 4;

            for (int i = 0; i < 11; i++)
            {
                if (i == 3 || i == 7) continue;
                int rows = (int)(scaledImage[i].Height / scale);
                int cols = (int)(scaledImage[i].Width / scale);
                Mat scaleBilinear = new Mat(rows, cols, DepthType.Cv64F, 1);
                CvInvoke.Resize(scaledImage[i], scaleBilinear, new Size(cols, rows), 0, 0, Inter.Linear);
                scaledImage[i + 1] = scaleBilinear;
                scale_factor[i + 1] = scale_factor[i] * scale;
            }
        }

        void ProcessScaledImage_v3(Mat[] scaledImage, float[] scale_factor, List<KeyValuePair<float, Rectangle>> rectDetected, float threshold, int from_index, int to_index, float compensation = 0F)
        {
            for (int t = to_index - 1; t >= from_index; t--)
            {
                Image<Gray, float> imgImage = scaledImage[t].ToImage<Gray, float>();
                int rows = imgImage.Height;
                int cols = imgImage.Width;

                //cal gradients in x and y direction
                float[,] Ix = new float[rows, cols];  //gradient in x direction
                float[,] Iy = new float[rows, cols];  //gradient in y direction
                int[,] quadrant_flag = new int[rows, cols];
                int[,] bin = new int[rows, cols];   //bin = [0;8]
                float[,] magnitude = new float[rows, cols];
                float a, b;

                for (int i = 0; i < rows - 1; i++)
                {
                    for (int j = 0; j < cols - 1; j++)
                    {
                        float I11, I12, I21, I22;
                        int q1, q2;

                        I11 = (float)imgImage[i, j + 1].Intensity;
                        if (j == 0) I12 = 0;
                        else I12 = (float)imgImage[i, j - 1].Intensity;
                        I21 = (float)imgImage[i + 1, j].Intensity;
                        if (i == 0) I22 = 0;
                        else I22 = (float)imgImage[i - 1, j].Intensity;

                        if (I11 > I12)
                        {
                            q1 = 1;
                            Ix[i, j] = I11 - I12;
                        }
                        else
                        {
                            q1 = -1;
                            Ix[i, j] = I12 - I11;
                        }
                        if (I21 > I22)
                        {
                            q2 = 1;
                            Iy[i, j] = I21 - I22;
                        }
                        else
                        {
                            q2 = -1;
                            Iy[i, j] = I22 - I21;
                        }
                        quadrant_flag[i, j] = (q1 * q2 > 0) ? 0 : 1;

                        a = Math.Max(Iy[i, j], Ix[i, j]);
                        b = Math.Min(Iy[i, j], Ix[i, j]);
                        magnitude[i, j] = Math.Max((float)(0.875 * a + 0.5 * b), a);

                        // da them thuat toan +90 degree vao de dam bao cac goc thuoc [0;180]
                        if (Ix[i, j] >= Iy[i, j] * tan[1])
                        {
                            if (Ix[i, j] >= Iy[i, j] * tan[2])
                            {
                                if (Ix[i, j] >= Iy[i, j] * tan[3])
                                {
                                    if (Ix[i, j] >= Iy[i, j] * tan[4])
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 0;
                                        else bin[i, j] = 8;
                                    }
                                    else
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 1;
                                        else bin[i, j] = 7;
                                    }
                                }
                                else
                                {
                                    if (quadrant_flag[i, j] == 1) bin[i, j] = 2;
                                    else bin[i, j] = 6;
                                }
                            }
                            else
                            {
                                if (quadrant_flag[i, j] == 1) bin[i, j] = 3;
                                else bin[i, j] = 5;
                            }
                        }
                        else
                        {
                            bin[i, j] = 4;
                        }
                    }
                }

                List<float> feature = new List<float>();
                //Iterations for Blocks
                for (int i = 0; i < rows / 8 - 1; i++)
                {
                    for (int j = 0; j < cols / 8 - 1; j++)
                    {
                        float[,] mag_patch = new float[16, 16];
                        int[,] bin_patch = new int[16, 16];
                        for (int u = 0; u < 16; u++)
                        {
                            for (int v = 0; v < 16; v++)
                            {
                                mag_patch[u, v] = magnitude[8 * i + u, 8 * j + v];
                                bin_patch[u, v] = bin[8 * i + u, 8 * j + v];
                            }
                        }
                        List<float> block_feature = new List<float>();

                        //Iterations for cells in a block
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                int[,] binA = new int[8, 8];
                                float[,] magA = new float[8, 8];
                                float[] histr = new float[9];

                                for (int u = 0; u < 8; u++)
                                {
                                    for (int v = 0; v < 8; v++)
                                    {
                                        binA[u, v] = bin_patch[8 * x + u, 8 * y + v];
                                        magA[u, v] = mag_patch[8 * x + u, 8 * y + v];
                                    }
                                }

                                //Iterations for pixels in one cell
                                for (int p = 0; p < 8; p++)
                                {
                                    for (int q = 0; q < 8; q++)
                                    {
                                        histr[binA[p, q]] += (float)(magA[p, q] * 0.5);
                                    }
                                }

                                //concatenation of 4 histograms to form one block feature
                                block_feature.AddRange(histr);
                            }
                        }

                        //Normalize the values in the block using Newton-Raphson method and John Carmack
                        //ComputeNormalizationUsingNewtonMethod(block_feature, 0.01, false);
                        //ComputeNormalizationUsingL2Norm(block_feature, 0.01, false);
                        ComputeNormalizationUsingL1Norm(block_feature, 0.01F);
                        feature.AddRange(block_feature);
                    }
                }

                //normalization of the feature vector using L2-Norm
                //if(ComputeNormalizationUsingNewtonMethod(feature, 0.001, true))
                //{
                //    ComputeNormalizationUsingNewtonMethod(feature, 0.001, false);
                //}
                if (ComputeNormalizationUsingL2Norm(feature, 0.001F, true))
                {
                    ComputeNormalizationUsingL2Norm(feature, 0.001F, false);
                }

                //multiply feature with SVM Coefficient corresponding and compare with threshold
                //float[] svm = HOGDescriptor.GetDefaultPeopleDetector();
                //process each window
                float maxAcc = 0;
                List<KeyValuePair<float, Rectangle>> tempRectDetected = new List<KeyValuePair<float, Rectangle>>();
                for (int i = 0; i < rows / 8 - 15; i++)
                {
                    for (int j = 0; j < cols / 8 - 7; j++)
                    {
                        float accumulated_result = 0;
                        //double[,,] window_feature = new double[15, 7, 36];
                        for (int u = 0; u < 15; u++)
                        {
                            for (int v = 0; v < 7; v++)
                            {
                                for (int e = 0; e < 36; e++)
                                {
                                    //window_feature[u, v, e] = feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e];
                                    //accumulated_result += window_feature[u, v, e] * svm[(u * 7 + v) * 36 + e];

                                    accumulated_result += feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e] * svm[(u * 7 + v) * 36 + e];
                                }
                            }
                        }
                        if (accumulated_result > maxAcc) maxAcc = accumulated_result;
                        if (accumulated_result >= threshold)
                        {
                            tempRectDetected.Add(new KeyValuePair<float, Rectangle>(accumulated_result, new Rectangle((int)(8 * j * scale_factor[t]), (int)(8 * i * scale_factor[t]), (int)(64 * scale_factor[t]), (int)(128 * scale_factor[t]))));
#if ShowResponseWindow
                            //display
                            Image<Gray, float> tempImg = scaledImage[t].ToImage<Gray, float>();
                            tempImg.Draw(new Rectangle(j * 8, i * 8, 64, 128), new Gray(255), 1);
                            ibImage.Image = tempImg.ToBitmap();
                            Thread.Sleep(1000);
#endif
                        }
                    }
                }
                txt2.AppendText("t=" + t.ToString() + "\tmaxAcc=" + maxAcc.ToString("n4") + "\n");
                foreach (KeyValuePair<float, Rectangle> p in tempRectDetected)
                {
#if DisplayMaxResults
                    if (p.Key >= (maxAcc - compensation))
                    {
                        rectDetected.Add(p);
                    }
#else   //display all the results
                    rectDetected.Add(p);
#endif
                }
            }
        }

        void ProcessImageUsingHardwareImplementationAlgorithm_v3()
        {
            Image ori = Image.FromFile(txtFile.Text);
            Mat oriImage = new Mat(txtFile.Text, LoadImageType.Grayscale);
            Mat[] scaledImage = new Mat[12];
            float[] scale_factor = new float[12];
            txt2.Text = "";

            float scale = (float)numScale.Value;
            float threshold = (float)numThreshold.Value;
            float compensation = (float)numCompensation.Value;

            if (scale <= 1) return;
            lblInfor.Text = oriImage.Height.ToString() + "x" + oriImage.Width.ToString();
            scaledImage[0] = oriImage.Clone();
            GenerateScaledImage_v3(scaledImage, scale_factor, scale);

            List<KeyValuePair<float, Rectangle>>[] rectDetected = new List<KeyValuePair<float, Rectangle>>[3];
            for (int i = 0; i < 3; i++)
            {
                rectDetected[i] = new List<KeyValuePair<float, Rectangle>>();
            }

            tProcess1 = new Thread(() => ProcessScaledImage_v3(scaledImage, scale_factor, rectDetected[0], threshold, 0, 1));
            tProcess2 = new Thread(() => ProcessScaledImage_v3(scaledImage, scale_factor, rectDetected[1], threshold, 1, 3));
            tProcess9 = new Thread(() => ProcessScaledImage_v3(scaledImage, scale_factor, rectDetected[2], threshold, 3, 12));
            tProcess1.Start(); tProcess2.Start(); tProcess9.Start();
            tProcess1.Join(); tProcess2.Join(); tProcess9.Join();

            ibImage.Image = DisplayResponse_v3(ori, rectDetected);
            isProcessing = false;
        }

        void ProcessScaledImage_v22(Mat[] scaledImage, double[] scale_factor, List<KeyValuePair<double, Rectangle>> rectDetected, double threshold, double compensation, int from_index, int to_index)
        {
            for (int t = to_index - 1; t >= from_index; t--)
            {
                Image<Gray, double> imgImage = scaledImage[t].ToImage<Gray, double>();
                int rows = imgImage.Height;
                int cols = imgImage.Width;

                //cal gradients in x and y direction
                double[,] Ix = new double[rows, cols];  //gradient in x direction
                double[,] Iy = new double[rows, cols];  //gradient in y direction
                int[,] quadrant_flag = new int[rows, cols];
                int[,] bin = new int[rows, cols];   //bin = [0;8]
                double[,] magnitude = new double[rows, cols];
                double a, b;

                for (int i = 0; i < rows - 2; i++)
                {
                    for (int j = 0; j < cols - 2; j++)
                    {
                        double I11 = imgImage[i, j].Intensity;
                        double I12 = imgImage[i, j + 2].Intensity;
                        double I21 = imgImage[i, j].Intensity;
                        double I22 = imgImage[i + 2, j].Intensity;
                        int q1, q2;
                        if (I11 > I12)
                        {
                            q1 = 1;
                            Ix[i, j] = I11 - I12;
                        }
                        else
                        {
                            q1 = -1;
                            Ix[i, j] = I12 - I11;
                        }
                        if (I21 > I22)
                        {
                            q2 = 1;
                            Iy[i, j] = I21 - I22;
                        }
                        else
                        {
                            q2 = -1;
                            Iy[i, j] = I22 - I21;
                        }
                        quadrant_flag[i, j] = (q1 * q2 > 0) ? 0 : 1;

                        a = Math.Max(Iy[i, j], Ix[i, j]);
                        b = Math.Min(Iy[i, j], Ix[i, j]);
                        magnitude[i, j] = Math.Max((0.875 * a + 0.5 * b), a);

                        // da them thuat toan +90 degree vao
                        if (Ix[i, j] >= Iy[i, j] * tan[1])
                        {
                            if (Ix[i, j] >= Iy[i, j] * tan[2])
                            {
                                if (Ix[i, j] >= Iy[i, j] * tan[3])
                                {
                                    if (Ix[i, j] >= Iy[i, j] * tan[4])
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 0;
                                        else bin[i, j] = 8;
                                    }
                                    else
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 1;
                                        else bin[i, j] = 7;
                                    }
                                }
                                else
                                {
                                    if (quadrant_flag[i, j] == 1) bin[i, j] = 2;
                                    else bin[i, j] = 6;
                                }
                            }
                            else
                            {
                                if (quadrant_flag[i, j] == 1) bin[i, j] = 3;
                                else bin[i, j] = 5;
                            }
                        }
                        else
                        {
                            bin[i, j] = 4;
                        }
                    }
                }

                List<double> feature = new List<double>();
                //Iterations for Blocks
                for (int i = 0; i < rows / 8 - 1; i++)
                {
                    for (int j = 0; j < cols / 8 - 1; j++)
                    {
                        double[,] mag_patch = new double[16, 16];
                        int[,] bin_patch = new int[16, 16];
                        for (int u = 0; u < 16; u++)
                        {
                            for (int v = 0; v < 16; v++)
                            {
                                mag_patch[u, v] = magnitude[8 * i + u, 8 * j + v];
                                bin_patch[u, v] = bin[8 * i + u, 8 * j + v];
                            }
                        }
                        List<double> block_feature = new List<double>();

                        //Iterations for cells in a block
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                int[,] binA = new int[8, 8];
                                double[,] magA = new double[8, 8];
                                double[] histr = new double[9];

                                for (int u = 0; u < 8; u++)
                                {
                                    for (int v = 0; v < 8; v++)
                                    {
                                        binA[u, v] = bin_patch[8 * x + u, 8 * y + v];
                                        magA[u, v] = mag_patch[8 * x + u, 8 * y + v];
                                    }
                                }

                                //Iterations for pixels in one cell
                                for (int p = 0; p < 8; p++)
                                {
                                    for (int q = 0; q < 8; q++)
                                    {
                                        histr[binA[p, q]] += magA[p, q] * 0.5;
                                    }
                                }

                                //concatenation of 4 histograms to form one block feature
                                block_feature.AddRange(histr);
                            }
                        }

                        //Normalize the values in the block using Newton-Raphson method and John Carmack
                        //ComputeNormalizationUsingNewtonMethod(block_feature, 0.01, false);
                        //ComputeNormalizationUsingL2Norm(block_feature, 0.01, false);
                        ComputeNormalizationUsingL1Norm(block_feature, 0.01);
                        feature.AddRange(block_feature);
                    }
                }

                //normalization of the feature vector using L2-Norm
                //if(ComputeNormalizationUsingNewtonMethod(feature, 0.001, true))
                //{
                //    ComputeNormalizationUsingNewtonMethod(feature, 0.001, false);
                //}
                if (ComputeNormalizationUsingL2Norm(feature, 0.001, true))
                {
                    ComputeNormalizationUsingL2Norm(feature, 0.001, false);
                }

                //multiply feature with SVM Coefficient corresponding and compare with threshold
                //float[] svm = HOGDescriptor.GetDefaultPeopleDetector();
                //process each window
                double maxAcc = 0;
                List<KeyValuePair<double, Rectangle>> tempRectDetected = new List<KeyValuePair<double, Rectangle>>();
                for (int i = 0; i < rows / 8 - 15; i++)
                {
                    for (int j = 0; j < cols / 8 - 7; j++)
                    {
                        double accumulated_result = 0;
                        //double[,,] window_feature = new double[15, 7, 36];
                        for (int u = 0; u < 15; u++)
                        {
                            for (int v = 0; v < 7; v++)
                            {
                                for (int e = 0; e < 36; e++)
                                {
                                    //window_feature[u, v, e] = feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e];
                                    //accumulated_result += window_feature[u, v, e] * svm[(u * 7 + v) * 36 + e];

                                    accumulated_result += feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e] * svm[(u * 7 + v) * 36 + e];
                                }
                            }
                        }
                        if (accumulated_result > maxAcc) maxAcc = accumulated_result;
#if ShowResponseWindow
                        //display
                        Image<Gray, double> tempImg = imgImage.Clone();
                        tempImg.Draw(new Rectangle(j * 8, i * 8, 64, 128), new Gray(255), 1);
                        //tempImg.Draw(new Rectangle((j + v) * 8, (i + u) * 8, 16, 16), new Gray(255), 1);
                        ibImage.Image = tempImg.ToBitmap();
                        //Thread.Sleep(50);
#endif
                        if (accumulated_result >= threshold)
                        {
                            //+1 for i and j to refuse the 0,0-case
                            tempRectDetected.Add(new KeyValuePair<double, Rectangle>(accumulated_result, new Rectangle((int)(8 * (j + 1) * scale_factor[t]), (int)(8 * (i + 1) * scale_factor[t]), (int)(64 * scale_factor[t]), (int)(128 * scale_factor[t]))));
                        }
                    }
                }
                txt2.AppendText("t=" + t.ToString() + "\tmaxAcc=" + maxAcc.ToString("n4") + "\n");
                foreach (KeyValuePair<double, Rectangle> p in tempRectDetected)
                {
                    if (p.Key >= (maxAcc - compensation))
                    {
                        rectDetected.Add(p);
                    }
                }
            }
        }

        void ProcessImageUsingHardwareImplementationAlgorithm_v22()
        {
            Image ori = Image.FromFile(txtFile.Text);
            Mat oriImage = new Mat(txtFile.Text, LoadImageType.Grayscale);
            Mat[] scaledImage = new Mat[12];
            double[] scale_factor = new double[12];
            txt2.Text = "";

            double scale = (double)numScale.Value;
            double threshold = (double)numThreshold.Value;
            double compensation = (double)numCompensation.Value;

            if (scale <= 1) return;
            lblInfor.Text = oriImage.Height.ToString() + "x" + oriImage.Width.ToString();

            scaledImage[0] = oriImage.Clone(); scale_factor[0] = 1;
            Mat down2x = new Mat(oriImage.Height / 2, oriImage.Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(oriImage, down2x);
            scaledImage[4] = down2x.Clone();//[4]
            scale_factor[4] = 2;
            Mat down4x = new Mat(down2x.Height / 2, down2x.Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(down2x, down4x);
            scaledImage[8] = down4x.Clone();//[8]
            scale_factor[8] = 4;

            for (int i = 0; i < 11; i++)
            {
                if (i == 3 || i == 7) continue;
                int rows = (int)(scaledImage[i].Height / scale);
                int cols = (int)(scaledImage[i].Width / scale);
                Mat scaleBilinear = new Mat(rows, cols, DepthType.Cv64F, 1);
                CvInvoke.Resize(scaledImage[i], scaleBilinear, new Size(cols, rows), 0, 0, Inter.Linear);
                scaledImage[i + 1] = scaleBilinear;
                scale_factor[i + 1] = scale_factor[i] * scale;
            }

            List<KeyValuePair<double, Rectangle>>[] rectDetected = new List<KeyValuePair<double, Rectangle>>[3];
            for (int i = 0; i < 3; i++)
            {
                rectDetected[i] = new List<KeyValuePair<double, Rectangle>>();
            }

            tProcess1 = new Thread(() => ProcessScaledImage_v22(scaledImage, scale_factor, rectDetected[0], threshold, compensation, 0, 1));
            tProcess2 = new Thread(() => ProcessScaledImage_v22(scaledImage, scale_factor, rectDetected[1], threshold, compensation, 1, 3));
            tProcess9 = new Thread(() => ProcessScaledImage_v22(scaledImage, scale_factor, rectDetected[2], threshold, compensation, 3, 12));
            tProcess1.Start(); tProcess2.Start(); tProcess9.Start();
            tProcess1.Join(); tProcess2.Join(); tProcess9.Join();

            ibImage.Image = DisplayResponse_v22(ori, rectDetected);
            isProcessing = false;
        }

        void ProcessImageUsingHardwareImplementationAlgorithm_v2()
        {
            Image ori = Image.FromFile(txtFile.Text);
            Mat oriImage = new Mat(txtFile.Text, LoadImageType.Grayscale);
            List<Mat> scaledImage = new List<Mat>(12);
            double[] scale_factor = new double[12];
            txt2.Text = "";

            double scale = (double)numScale.Value;
            double threshold = (double)numThreshold.Value;
            double compensation = (double)numCompensation.Value;

            if (scale <= 1) return;
            lblInfor.Text = oriImage.Height.ToString() + "x" + oriImage.Width.ToString();

            scaledImage.Add(oriImage.Clone());//[0]
            scale_factor[0] = 1;
            for (int i = 0; i < 3; i++)
            {
                int rows = (int)(scaledImage[i].Height / scale);
                int cols = (int)(scaledImage[i].Width / scale);
                Mat scaleBilinear = new Mat(rows, cols, DepthType.Cv64F, 1);
                CvInvoke.Resize(scaledImage[i], scaleBilinear, new Size(cols, rows), 0, 0, Inter.Linear);
                scaledImage.Add(scaleBilinear);//[1,2,3]
                scale_factor[i + 1] = scale_factor[i] * scale;
            }

            Mat down2x = new Mat(oriImage.Height / 2, oriImage.Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(oriImage, down2x);
            scaledImage.Add(down2x.Clone());//[4]
            scale_factor[4] = 2;
            for (int i = 4; i < 7; i++)
            {
                int rows = (int)(scaledImage[i].Height / scale);
                int cols = (int)(scaledImage[i].Width / scale);
                Mat scaleBilinear = new Mat(rows, cols, DepthType.Cv64F, 1);
                CvInvoke.Resize(scaledImage[i], scaleBilinear, new Size(cols, rows), 0, 0, Inter.Linear);
                scaledImage.Add(scaleBilinear);//[5,6,7]
                scale_factor[i + 1] = scale_factor[i] * scale;
            }

            Mat down4x = new Mat(down2x.Height / 2, down2x.Width / 2, DepthType.Cv64F, 1);
            CvInvoke.PyrDown(down2x, down4x);
            scaledImage.Add(down4x.Clone());//[8]
            scale_factor[8] = 4;
            for (int i = 8; i < 11; i++)
            {
                int rows = (int)(scaledImage[i].Height / scale);
                int cols = (int)(scaledImage[i].Width / scale);
                Mat scaleBilinear = new Mat(rows, cols, DepthType.Cv64F, 1);
                CvInvoke.Resize(scaledImage[i], scaleBilinear, new Size(cols, rows), 0, 0, Inter.Linear);
                scaledImage.Add(scaleBilinear);//[9,10,11]
                scale_factor[i + 1] = scale_factor[i] * scale;
            }

            List<KeyValuePair<double, Rectangle>> rectDetected = new List<KeyValuePair<double, Rectangle>>();
            double maxMaxAcc = 0;
            for (int t = 11; t >= 0; t--)
            {
                Image<Gray, double> imgImage = scaledImage[t].ToImage<Gray, double>();
                int rows = imgImage.Height;
                int cols = imgImage.Width;

                //cal gradients in x and y direction
                double[,] Ix = new double[rows, cols];  //gradient in x direction
                double[,] Iy = new double[rows, cols];  //gradient in y direction
                int[,] quadrant_flag = new int[rows, cols];
                int[,] bin = new int[rows, cols];   //bin = [0;8]
                double[,] magnitude = new double[rows, cols];
                double a, b;
                for (int i = 0; i < rows - 2; i++)
                {
                    for (int j = 0; j < cols - 2; j++)
                    {
                        double I11 = imgImage[i, j].Intensity;
                        double I12 = imgImage[i, j + 2].Intensity;
                        double I21 = imgImage[i, j].Intensity;
                        double I22 = imgImage[i + 2, j].Intensity;
                        int q1, q2;
                        if (I11 > I12)
                        {
                            q1 = 1;
                            Ix[i, j] = I11 - I12;
                        }
                        else
                        {
                            q1 = -1;
                            Ix[i, j] = I12 - I11;
                        }
                        if (I21 > I22)
                        {
                            q2 = 1;
                            Iy[i, j] = I21 - I22;
                        }
                        else
                        {
                            q2 = -1;
                            Iy[i, j] = I22 - I21;
                        }
                        quadrant_flag[i, j] = (q1 * q2 > 0) ? 0 : 1;

                        a = Math.Max(Iy[i, j], Ix[i, j]);
                        b = Math.Min(Iy[i, j], Ix[i, j]);
                        magnitude[i, j] = Math.Max((0.875 * a + 0.5 * b), a);

                        // da them thuat toan +90 degree vao
                        if (Ix[i, j] >= Iy[i, j] * tan[1])
                        {
                            if (Ix[i, j] >= Iy[i, j] * tan[2])
                            {
                                if (Ix[i, j] >= Iy[i, j] * tan[3])
                                {
                                    if (Ix[i, j] >= Iy[i, j] * tan[4])
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 0;
                                        else bin[i, j] = 8;
                                    }
                                    else
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 1;
                                        else bin[i, j] = 7;
                                    }
                                }
                                else
                                {
                                    if (quadrant_flag[i, j] == 1) bin[i, j] = 2;
                                    else bin[i, j] = 6;
                                }
                            }
                            else
                            {
                                if (quadrant_flag[i, j] == 1) bin[i, j] = 3;
                                else bin[i, j] = 5;
                            }
                        }
                        else
                        {
                            bin[i, j] = 4;
                        }
                    }
                }

                List<double> feature = new List<double>();

                //Iterations for Blocks
                for (int i = 0; i < rows / 8 - 1; i++)
                {
                    for (int j = 0; j < cols / 8 - 1; j++)
                    {
                        double[,] mag_patch = new double[16, 16];
                        int[,] bin_patch = new int[16, 16];
                        for (int u = 0; u < 16; u++)
                        {
                            for (int v = 0; v < 16; v++)
                            {
                                mag_patch[u, v] = magnitude[8 * i + u, 8 * j + v];
                                bin_patch[u, v] = bin[8 * i + u, 8 * j + v];
                            }
                        }
                        List<double> block_feature = new List<double>();

                        //Iterations for cells in a block
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                int[,] binA = new int[8, 8];
                                double[,] magA = new double[8, 8];
                                double[] histr = new double[9];

                                for (int u = 0; u < 8; u++)
                                {
                                    for (int v = 0; v < 8; v++)
                                    {
                                        binA[u, v] = bin_patch[8 * x + u, 8 * y + v];
                                        magA[u, v] = mag_patch[8 * x + u, 8 * y + v];
                                    }
                                }

                                //Iterations for pixels in one cell
                                for (int p = 0; p < 8; p++)
                                {
                                    for (int q = 0; q < 8; q++)
                                    {
                                        histr[binA[p, q]] += magA[p, q] * 0.5;
                                    }
                                }

                                //concatenation of 4 histograms to form one block feature
                                block_feature.AddRange(histr);
                            }
                        }

                        //Normalize the values in the block using Newton-Raphson method and John Carmack
                        //ComputeNormalizationUsingNewtonMethod(block_feature, 0.01, false);
                        //ComputeNormalizationUsingL2Norm(block_feature, 0.01, false);
                        ComputeNormalizationUsingL1Norm(block_feature, 0.01);

                        feature.AddRange(block_feature);
                    }
                }

                //normalization of the feature vector using L2-Norm
                //if(ComputeNormalizationUsingNewtonMethod(feature, 0.001, true))
                //{
                //    ComputeNormalizationUsingNewtonMethod(feature, 0.001, false);
                //}
                if (ComputeNormalizationUsingL2Norm(feature, 0.001, true))
                {
                    ComputeNormalizationUsingL2Norm(feature, 0.001, false);
                }

                //multiply feature with SVM Coefficient corresponding and compare with threshold
                //float[] svm = HOGDescriptor.GetDefaultPeopleDetector();

                //process each window
                double maxAcc = 0;
                List<KeyValuePair<double, Rectangle>> tempRectDetected = new List<KeyValuePair<double, Rectangle>>();
                for (int i = 0; i < rows / 8 - 15; i++)
                {
                    for (int j = 0; j < cols / 8 - 7; j++)
                    {
                        double accumulated_result = 0;
                        //double[,,] window_feature = new double[15, 7, 36];
                        for (int u = 0; u < 15; u++)
                        {
                            for (int v = 0; v < 7; v++)
                            {
                                for (int e = 0; e < 36; e++)
                                {
                                    //window_feature[u, v, e] = feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e];
                                    //accumulated_result += window_feature[u, v, e] * svm[(u * 7 + v) * 36 + e];

                                    accumulated_result += feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e] * svm[(u * 7 + v) * 36 + e];
                                }
                            }
                        }
                        if (accumulated_result > maxAcc) maxAcc = accumulated_result;

#if ShowResponseWindow
                        //display
                        Image<Gray, double> tempImg = imgImage.Clone();
                        tempImg.Draw(new Rectangle(j * 8, i * 8, 64, 128), new Gray(255), 1);
                        //tempImg.Draw(new Rectangle((j + v) * 8, (i + u) * 8, 16, 16), new Gray(255), 1);
                        ibImage.Image = tempImg.ToBitmap();
                        //Thread.Sleep(50);
#endif

                        if (accumulated_result >= threshold)
                        {
                            //+1 for i and j to refuse the 0,0-case
                            tempRectDetected.Add(new KeyValuePair<double, Rectangle>(accumulated_result, new Rectangle((int)(8 * (j + 1) * scale_factor[t]), (int)(8 * (i + 1) * scale_factor[t]), (int)(64 * scale_factor[t]), (int)(128 * scale_factor[t]))));
                        }
                    }
                }

                txt2.AppendText("t=" + t.ToString() + "\tmaxAcc=" + maxAcc.ToString("n4") + "\n");
                foreach (KeyValuePair<double, Rectangle> p in tempRectDetected)
                {
                    if (p.Key >= (maxAcc - compensation))
                    {
                        rectDetected.Add(p);
                    }
                }

                if (maxAcc > maxMaxAcc) maxMaxAcc = maxAcc;
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            ibImage.Image = DisplayResponse(ori, rectDetected);

            isProcessing = false;
        }

        void ProcessImageUsingHardwareImplementationAlgorithm()
        {
            Image ori = Image.FromFile(txtFile.Text);
            Image<Bgr, byte> oriBgrImage = new Image<Bgr, byte>(txtFile.Text);
            Image<Gray, double> oriImage = (oriBgrImage.Convert<Gray, byte>()).Convert<Gray, double>();
            //process 8 scaled images (for ped 10.jpg)
            List<Image<Gray, double>> scaledImage = new List<Image<Gray, double>>();
            txt2.Text = "";

            double scale = (double)numScale.Value;
            double threshold = (double)numThreshold.Value;
            double compensation = (double)numCompensation.Value;

            if (scale <= 1) return;
            scaledImage.Add(oriImage.Clone());
            int tRow = 0, tCol = 0, tOrder = 0;
            tRow = scaledImage[tOrder].Height;
            tCol = scaledImage[tOrder].Width;
            lblInfor.Text = tRow.ToString() + "x" + tCol.ToString();

            while ((tRow / scale) > 128 && (tCol / scale) > 64)
            {
                scaledImage.Add(scaledImage[tOrder].Clone().Resize(1 / scale, Inter.Cubic));
                tRow = scaledImage[tOrder].Height;
                tCol = scaledImage[tOrder].Width;
                tOrder++;
            }

            double[] scale_factor = new double[++tOrder];
            scale_factor[0] = 1;
            for (int i = 1; i < tOrder; i++)
            {
                scale_factor[i] = scale_factor[i - 1] * scale;
            }

            List<KeyValuePair<double, Rectangle>> rectDetected = new List<KeyValuePair<double, Rectangle>>();
            double maxMaxAcc = 0;
            for (int t = tOrder - 1; t >= 0; t--)
            {
                Image<Gray, double> imgImage = scaledImage[t].Clone();
                int rows = imgImage.Height;
                int cols = imgImage.Width;

                //cal gradients in x and y direction
                double[,] Ix = new double[rows, cols];  //gradient in x direction
                double[,] Iy = new double[rows, cols];  //gradient in y direction
                int[,] quadrant_flag = new int[rows, cols];
                int[,] bin = new int[rows, cols];   //bin = [0;8]
                double[,] magnitude = new double[rows, cols];
                double a, b;
                for (int i = 0; i < rows - 2; i++)
                {
                    for (int j = 0; j < cols - 2; j++)
                    {
                        double I11 = imgImage[i, j].Intensity;
                        double I12 = imgImage[i, j + 2].Intensity;
                        double I21 = imgImage[i, j].Intensity;
                        double I22 = imgImage[i + 2, j].Intensity;
                        int q1, q2;
                        if (I11 > I12)
                        {
                            q1 = 1;
                            Ix[i, j] = I11 - I12;
                        }
                        else
                        {
                            q1 = -1;
                            Ix[i, j] = I12 - I11;
                        }
                        if (I21 > I22)
                        {
                            q2 = 1;
                            Iy[i, j] = I21 - I22;
                        }
                        else
                        {
                            q2 = -1;
                            Iy[i, j] = I22 - I21;
                        }
                        quadrant_flag[i, j] = (q1 * q2 > 0) ? 0 : 1;

                        a = Math.Max(Iy[i, j], Ix[i, j]);
                        b = Math.Min(Iy[i, j], Ix[i, j]);
                        magnitude[i, j] = Math.Max((0.875 * a + 0.5 * b), a);

                        // da them thuat toan +90 degree vao
                        if (Ix[i, j] >= Iy[i, j] * tan[1])
                        {
                            if (Ix[i, j] >= Iy[i, j] * tan[2])
                            {
                                if (Ix[i, j] >= Iy[i, j] * tan[3])
                                {
                                    if (Ix[i, j] >= Iy[i, j] * tan[4])
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 0;
                                        else bin[i, j] = 8;
                                    }
                                    else
                                    {
                                        if (quadrant_flag[i, j] == 1) bin[i, j] = 1;
                                        else bin[i, j] = 7;
                                    }
                                }
                                else
                                {
                                    if (quadrant_flag[i, j] == 1) bin[i, j] = 2;
                                    else bin[i, j] = 6;
                                }
                            }
                            else
                            {
                                if (quadrant_flag[i, j] == 1) bin[i, j] = 3;
                                else bin[i, j] = 5;
                            }
                        }
                        else
                        {
                            bin[i, j] = 4;
                        }
                    }
                }

                List<double> feature = new List<double>();

                //Iterations for Blocks
                for (int i = 0; i < rows / 8 - 1; i++)
                {
                    for (int j = 0; j < cols / 8 - 1; j++)
                    {
                        double[,] mag_patch = new double[16, 16];
                        int[,] bin_patch = new int[16, 16];
                        for (int u = 0; u < 16; u++)
                        {
                            for (int v = 0; v < 16; v++)
                            {
                                mag_patch[u, v] = magnitude[8 * i + u, 8 * j + v];
                                bin_patch[u, v] = bin[8 * i + u, 8 * j + v];
                            }
                        }
                        List<double> block_feature = new List<double>();

                        //Iterations for cells in a block
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                int[,] binA = new int[8, 8];
                                double[,] magA = new double[8, 8];
                                double[] histr = new double[9];

                                for (int u = 0; u < 8; u++)
                                {
                                    for (int v = 0; v < 8; v++)
                                    {
                                        binA[u, v] = bin_patch[8 * x + u, 8 * y + v];
                                        magA[u, v] = mag_patch[8 * x + u, 8 * y + v];
                                    }
                                }

                                //Iterations for pixels in one cell
                                for (int p = 0; p < 8; p++)
                                {
                                    for (int q = 0; q < 8; q++)
                                    {
                                        histr[binA[p, q]] += magA[p, q] * 0.5;
                                    }
                                }

                                //concatenation of 4 histograms to form one block feature
                                block_feature.AddRange(histr);
                            }
                        }

                        //Normalize the values in the block using Newton-Raphson method and John Carmack
                        //ComputeNormalizationUsingNewtonMethod(block_feature, 0.01, false);
                        //ComputeNormalizationUsingL2Norm(block_feature, 0.01, false);
                        ComputeNormalizationUsingL1Norm(block_feature, 0.01);

                        feature.AddRange(block_feature);
                    }
                }

                //normalization of the feature vector using L2-Norm
                //if(ComputeNormalizationUsingNewtonMethod(feature, 0.001, true))
                //{
                //    ComputeNormalizationUsingNewtonMethod(feature, 0.001, false);
                //}
                if (ComputeNormalizationUsingL2Norm(feature, 0.001, true))
                {
                    ComputeNormalizationUsingL2Norm(feature, 0.001, false);
                }

                //multiply feature with SVM Coefficient corresponding and compare with threshold
                //float[] svm = HOGDescriptor.GetDefaultPeopleDetector();

                //process each window
                double maxAcc = 0;
                List<KeyValuePair<double, Rectangle>> tempRectDetected = new List<KeyValuePair<double, Rectangle>>();
                for (int i = 0; i < rows / 8 - 15; i++)
                {
                    for (int j = 0; j < cols / 8 - 7; j++)
                    {
                        double accumulated_result = 0;
                        //double[,,] window_feature = new double[15, 7, 36];
                        for (int u = 0; u < 15; u++)
                        {
                            for (int v = 0; v < 7; v++)
                            {
                                for (int e = 0; e < 36; e++)
                                {
                                    //window_feature[u, v, e] = feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e];
                                    //accumulated_result += window_feature[u, v, e] * svm[(u * 7 + v) * 36 + e];

                                    accumulated_result += feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e] * svm[(u * 7 + v) * 36 + e];
                                }
                            }
                        }
                        if (accumulated_result > maxAcc) maxAcc = accumulated_result;

#if ShowResponseWindow
                        //display
                        Image<Gray, double> tempImg = imgImage.Clone();
                        tempImg.Draw(new Rectangle(j * 8, i * 8, 64, 128), new Gray(255), 1);
                        //tempImg.Draw(new Rectangle((j + v) * 8, (i + u) * 8, 16, 16), new Gray(255), 1);
                        ibImage.Image = tempImg.ToBitmap();
                        //Thread.Sleep(50);
#endif

                        if (accumulated_result >= threshold)
                        {
                            tempRectDetected.Add(new KeyValuePair<double, Rectangle>(accumulated_result, new Rectangle((int)(8 * j * scale_factor[t]), (int)(8 * i * scale_factor[t]), (int)(64 * scale_factor[t]), (int)(128 * scale_factor[t]))));
                        }
                    }
                }

                txt2.AppendText("t=" + t.ToString() + "\tmaxAcc=" + maxAcc.ToString("n4") + "\n");
                foreach (KeyValuePair<double, Rectangle> p in tempRectDetected)
                {
                    if (p.Key >= (maxAcc - compensation))
                    {
                        rectDetected.Add(p);
                    }
                }

                if (maxAcc > maxMaxAcc) maxMaxAcc = maxAcc;
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            ibImage.Image = DisplayResponse(ori, rectDetected);

            isProcessing = false;
        }

        void ProcessImageUsingOriginalHOGAlgorithm()
        {
            Image ori = Image.FromFile(txtFile.Text);
            Image<Bgr, byte> oriBgrImage = new Image<Bgr, byte>(txtFile.Text);
            Image<Gray, double> oriImage = (oriBgrImage.Convert<Gray, byte>()).Convert<Gray, double>();
            //process 8 scaled images (for ped 10.jpg)
            List<Image<Gray, double>> scaledImage = new List<Image<Gray, double>>();
            txt2.Text = "";

            double scale = (double)numScale.Value;
            double threshold = (double)numThreshold.Value;
            double compensation = (double)numCompensation.Value;

            if (scale <= 1) return;
            scaledImage.Add(oriImage.Clone());
            int tRow = 0, tCol = 0, tOrder = 0;
            tRow = scaledImage[tOrder].Height;
            tCol = scaledImage[tOrder].Width;
            lblInfor.Text = tRow.ToString() + "x" + tCol.ToString();

            while ((tRow / scale) > 128 && (tCol / scale) > 64)
            {
                scaledImage.Add(scaledImage[tOrder].Clone().Resize(1 / scale, Inter.Cubic));
                tRow = scaledImage[tOrder].Height;
                tCol = scaledImage[tOrder].Width;
                tOrder++;
            }

            double[] scale_factor = new double[++tOrder];
            scale_factor[0] = 1;
            for (int i = 1; i < tOrder; i++)
            {
                scale_factor[i] = scale_factor[i - 1] * scale;
            }

            List<KeyValuePair<double, Rectangle>> rectDetected = new List<KeyValuePair<double, Rectangle>>();
            double maxMaxAcc = 0;
            for (int t = tOrder - 1; t >= 0; t--)
            {
                Image<Gray, double> imgImage = scaledImage[t].Clone();
                int rows = imgImage.Height;
                int cols = imgImage.Width;

                //cal gradients in x and y direction
                double[,] Ix = new double[rows, cols];  //gradient in x direction
                double[,] Iy = new double[rows, cols];  //gradient in y direction
                double[,] angle = new double[rows, cols];
                double[,] magnitude = new double[rows, cols];
                for (int i = 0; i < rows - 2; i++)
                {
                    for (int j = 0; j < cols - 2; j++)
                    {
                        Ix[i, j] = imgImage[i, j].Intensity - imgImage[i, j + 2].Intensity;
                        Iy[i, j] = imgImage[i, j].Intensity - imgImage[i + 2, j].Intensity;
                        if (Iy[i, j] != 0)
                        {
                            angle[i, j] = Math.Atan(Ix[i, j] / Iy[i, j]) * 180 / Math.PI;
                            angle[i, j] += 90;
                        }
                        else
                        {
                            angle[i, j] = 0;
                        }
                        magnitude[i, j] = Math.Sqrt(Ix[i, j] * Ix[i, j] + Iy[i, j] * Iy[i, j]);
                    }
                }

                List<double> feature = new List<double>();

                //Iterations for Blocks
                for (int i = 0; i < rows / 8 - 1; i++)
                {
                    for (int j = 0; j < cols / 8 - 1; j++)
                    {
                        double[,] mag_patch = new double[16, 16];
                        double[,] ang_patch = new double[16, 16];
                        for (int u = 0; u < 16; u++)
                        {
                            for (int v = 0; v < 16; v++)
                            {
                                mag_patch[u, v] = magnitude[8 * i + u, 8 * j + v];
                                ang_patch[u, v] = angle[8 * i + u, 8 * j + v];
                            }
                        }
                        List<double> block_feature = new List<double>();

                        //Iterations for cells in a block
                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                double[,] angleA = new double[8, 8];
                                double[,] magA = new double[8, 8];
                                double[] histr = new double[9];

                                for (int a = 0; a < 8; a++)
                                {
                                    for (int b = 0; b < 8; b++)
                                    {
                                        angleA[a, b] = ang_patch[8 * x + a, 8 * y + b];
                                        magA[a, b] = mag_patch[8 * x + a, 8 * y + b];
                                    }
                                }

                                //Iterations for pixels in one cell
                                for (int p = 0; p < 8; p++)
                                {
                                    for (int q = 0; q < 8; q++)
                                    {
                                        double alpha = angleA[p, q];

                                        //Binning Process (Bi-Linear Interpolation)
                                        if (alpha > 10 && alpha <= 30)
                                        {
                                            histr[0] += magA[p, q] * (30 - alpha) / 20;
                                            histr[1] += magA[p, q] * (alpha - 10) / 20;
                                        }
                                        else if (alpha > 30 && alpha <= 50)
                                        {
                                            histr[1] += magA[p, q] * (50 - alpha) / 20;
                                            histr[2] += magA[p, q] * (alpha - 30) / 20;
                                        }
                                        else if (alpha > 50 && alpha <= 70)
                                        {
                                            histr[2] += magA[p, q] * (70 - alpha) / 20;
                                            histr[3] += magA[p, q] * (alpha - 50) / 20;
                                        }
                                        else if (alpha > 70 && alpha <= 90)
                                        {
                                            histr[3] += magA[p, q] * (90 - alpha) / 20;
                                            histr[4] += magA[p, q] * (alpha - 70) / 20;
                                        }
                                        else if (alpha > 90 && alpha <= 110)
                                        {
                                            histr[4] += magA[p, q] * (110 - alpha) / 20;
                                            histr[5] += magA[p, q] * (alpha - 90) / 20;
                                        }
                                        else if (alpha > 110 && alpha <= 130)
                                        {
                                            histr[5] += magA[p, q] * (130 - alpha) / 20;
                                            histr[6] += magA[p, q] * (alpha - 110) / 20;
                                        }
                                        else if (alpha > 130 && alpha <= 150)
                                        {
                                            histr[6] += magA[p, q] * (150 - alpha) / 20;
                                            histr[7] += magA[p, q] * (alpha - 130) / 20;
                                        }
                                        else if (alpha > 150 && alpha <= 170)
                                        {
                                            histr[7] += magA[p, q] * (170 - alpha) / 20;
                                            histr[8] += magA[p, q] * (alpha - 150) / 20;
                                        }
                                        else if (alpha > 170 && alpha <= 180)
                                        {
                                            histr[8] += magA[p, q] * (190 - alpha) / 20;
                                            histr[0] += magA[p, q] * (alpha - 170) / 20;
                                        }
                                        else if (alpha >= 0 && alpha <= 10)
                                        {
                                            histr[0] += magA[p, q] * (alpha + 10) / 20;
                                            histr[8] += magA[p, q] * (10 - alpha) / 20;
                                        }
                                    }
                                }

                                //concatenation of 4 histograms to form one block feature
                                block_feature.AddRange(histr);
                            }
                        }

                        //Normalize the values in the block using L1-Norm
                        //ComputeNormalizationUsingL2Norm(block_feature, 0.01, false);
                        ComputeNormalizationUsingL1Norm(block_feature, 0.01);

                        feature.AddRange(block_feature);
                    }
                }

                //normalization of the feature vector using L2-Norm
                if (ComputeNormalizationUsingL2Norm(feature, 0.001, true))
                {
                    ComputeNormalizationUsingL2Norm(feature, 0.001, false);
                }

                //multiply feature with SVM Coefficient corresponding and compare with threshold
                //float[] svm = HOGDescriptor.GetDefaultPeopleDetector();

                //process each window
                double maxAcc = 0;
                List<KeyValuePair<double, Rectangle>> tempRectDetected = new List<KeyValuePair<double, Rectangle>>();
                for (int i = 0; i < rows / 8 - 15; i++)
                {
                    for (int j = 0; j < cols / 8 - 7; j++)
                    {
                        double accumulated_result = 0;
                        //double[,,] window_feature = new double[15, 7, 36];
                        for (int u = 0; u < 15; u++)
                        {
                            for (int v = 0; v < 7; v++)
                            {
                                for (int e = 0; e < 36; e++)
                                {
                                    //window_feature[u, v, e] = feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e];
                                    //accumulated_result += window_feature[u, v, e] * svm[(u * 7 + v) * 36 + e];

                                    accumulated_result += feature[((i + u) * (cols / 8 - 1) + j + v) * 36 + e] * svm[(u * 7 + v) * 36 + e];
                                }
                            }
                        }
                        if (accumulated_result > maxAcc) maxAcc = accumulated_result;

#if ShowResponseWindow
                        //display
                        Image<Gray, double> tempImg = imgImage.Clone();
                        tempImg.Draw(new Rectangle(j * 8, i * 8, 64, 128), new Gray(255), 1);
                        //tempImg.Draw(new Rectangle((j + v) * 8, (i + u) * 8, 16, 16), new Gray(255), 1);
                        ibImage.Image = tempImg.ToBitmap();
                        //Thread.Sleep(50);
#endif

                        if (accumulated_result >= threshold)
                        {
                            tempRectDetected.Add(new KeyValuePair<double, Rectangle>(accumulated_result, new Rectangle((int)(8 * j * scale_factor[t]), (int)(8 * i * scale_factor[t]), (int)(64 * scale_factor[t]), (int)(128 * scale_factor[t]))));
                        }
                    }
                }

                txt2.AppendText("t=" + t.ToString() + "\tmaxAcc=" + maxAcc.ToString("n4") + "\n");
                foreach (KeyValuePair<double, Rectangle> p in tempRectDetected)
                {
                    if (p.Key >= (maxAcc - compensation))
                    {
                        rectDetected.Add(p);
                    }
                }

                if (maxAcc > maxMaxAcc) maxMaxAcc = maxAcc;
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            ibImage.Image = DisplayResponse(ori, rectDetected);

            isProcessing = false;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (isProcessing) return;
            if (txtFile.Text != String.Empty)
            {
                isProcessing = true;
                //ProcessImageAndUpdateGUI();
                //ProcessImageUsingHardwareImplementationAlgorithm();
                //ProcessImageUsingOriginalHOGAlgorithm();

                //tProcess = new Thread(new ThreadStart(ProcessImageAndUpdateGUI));
                //tProcess = new Thread(new ThreadStart(ProcessImageUsingHardwareImplementationAlgorithm));
                //tProcess = new Thread(new ThreadStart(ProcessImageUsingHardwareImplementationAlgorithm_v2));
                //tProcess = new Thread(new ThreadStart(ProcessImageUsingHardwareImplementationAlgorithm_v22));
                tProcess = new Thread(new ThreadStart(ProcessImageUsingHardwareImplementationAlgorithm_v3));
                //tProcess = new Thread(new ThreadStart(ProcessImageUsingOriginalHOGAlgorithm));
                tProcess.Start();
            }
        }

        void MergeResponse_v3(List<KeyValuePair<float, Rectangle>> rect, List<KeyValuePair<float, Rectangle>> mergeList)
        {
            while (rect.Count > 0)
            {
                if (rect.Count == 1)
                {
                    mergeList.Add(rect[0]);
                    rect.RemoveAt(0);
                    break;
                }
                int x1 = rect[0].Value.X;
                int y1 = rect[0].Value.Y;
                int w1Thres = rect[0].Value.Width / 2;
                int h1Thres = rect[0].Value.Height / 2;
                List<int> chooseList = new List<int>();
                chooseList.Add(0);

                //chon ra cac rect co the merge
                for (int j = 1; j < rect.Count; j++)
                {
                    if (rect[j].Value.X <= (x1 + w1Thres))
                    {
                        if ((rect[j].Value.Y <= (y1 + h1Thres)) || (rect[j].Value.Y >= (y1 - h1Thres)))
                        {
                            chooseList.Add(j);
                        }
                    }
                    else break;
                }
                //chon ra rect co result lon nhat
                int idx = 0;
                float maxAcc = rect[0].Key;
                foreach (int item in chooseList)
                {
                    if (rect[item].Key > maxAcc)
                    {
                        maxAcc = rect[item].Key;
                        idx = item;
                    }
                }
                mergeList.Add(rect[idx]);

                //loai bo cac rect vua xu ly
                int i = 0;
                foreach (int item in chooseList)
                {
                    rect.RemoveAt(item - i);
                    i++;
                }
            }
        }


        Image DisplayResponse_v3(Image ori, List<KeyValuePair<float, Rectangle>>[] rectDetected)
        {
            //Add all rect into a list
            List<KeyValuePair<float, Rectangle>> rect = new List<KeyValuePair<float, Rectangle>>();
            foreach (List<KeyValuePair<float, Rectangle>> l in rectDetected)
            {
                foreach (KeyValuePair<float, Rectangle> p in l)
                {
                    rect.Add(p);
                }
            }
            //sap xep list theo toa do upper-left corner cua Rect
            rect.Sort((x, y) => x.Value.X.CompareTo(y.Value.X));
            List<KeyValuePair<float, Rectangle>> mergeList = new List<KeyValuePair<float, Rectangle>>();
            MergeResponse_v3(rect, mergeList);
            //loc lan 2
            List<KeyValuePair<float, Rectangle>> mergeList2 = new List<KeyValuePair<float, Rectangle>>();
            MergeResponse_v3(mergeList, mergeList2);


            //Display
            float maxMaxAcc = 0;
            Color customColor = Color.FromArgb(90, Color.LightGreen);
            SolidBrush shadowBrush = new SolidBrush(customColor);
            Brush br = Brushes.Red;
            Font ff = new Font("Tahoma", 10, FontStyle.Bold);
            using (Graphics g = Graphics.FromImage(ori))
            {
                foreach (KeyValuePair<float, Rectangle> p in mergeList2)
                {
                    g.FillRectangles(shadowBrush, new Rectangle[] { p.Value });
                    g.DrawString(p.Key.ToString("n4"), ff, br, new Point(p.Value.X + 1, p.Value.Y + 1));
                    if (p.Key > maxMaxAcc) maxMaxAcc = p.Key;
                }
                g.Flush();
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            return ori;
        }

        //DisplayResponse for MaxResults
        Image DisplayResponse(Image ori, List<KeyValuePair<float, Rectangle>>[] rectDetected)
        {
            float maxMaxAcc = 0;
            Color customColor = Color.FromArgb(70, Color.LightGreen);
            SolidBrush shadowBrush = new SolidBrush(customColor);
            Brush br = Brushes.Red;
            Font ff = new Font("Tahoma", 10, FontStyle.Bold);
            using (Graphics g = Graphics.FromImage(ori))
            {
                foreach (List<KeyValuePair<float, Rectangle>> l in rectDetected)
                {
                    foreach (KeyValuePair<float, Rectangle> p in l)
                    {
                        g.FillRectangles(shadowBrush, new Rectangle[] { p.Value });
                        g.DrawString(p.Key.ToString("n4"), ff, br, new Point(p.Value.X + 1, p.Value.Y + 1));
                        if (p.Key > maxMaxAcc) maxMaxAcc = p.Key;
                    }
                }

                g.Flush();
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            return ori;
        }

        Image DisplayResponse_v22(Image ori, List<KeyValuePair<double, Rectangle>>[] rectDetected)
        {
            double maxMaxAcc = 0;
            Color customColor = Color.FromArgb(70, Color.LightGreen);
            SolidBrush shadowBrush = new SolidBrush(customColor);
            Brush br = Brushes.Red;
            Font ff = new Font("Tahoma", 10, FontStyle.Bold);
            using (Graphics g = Graphics.FromImage(ori))
            {
                foreach (List<KeyValuePair<double, Rectangle>> l in rectDetected)
                {
                    foreach (KeyValuePair<double, Rectangle> p in l)
                    {
                        g.FillRectangles(shadowBrush, new Rectangle[] { p.Value });
                        g.DrawString(p.Key.ToString("n4"), ff, br, new Point(p.Value.X + 1, p.Value.Y + 1));
                        if (p.Key > maxMaxAcc) maxMaxAcc = p.Key;
                    }
                }

                g.Flush();
            }
            txt2.AppendText("maxMaxAcc=" + maxMaxAcc.ToString("n4") + "\n");
            return ori;
        }

        Image DisplayResponse(Image ori, List<KeyValuePair<double, Rectangle>> rectDetected)
        {
            Color customColor = Color.FromArgb(70, Color.LightGreen);
            SolidBrush shadowBrush = new SolidBrush(customColor);
            Brush br = Brushes.Red;
            Font ff = new Font("Tahoma", 10, FontStyle.Bold);
            using (Graphics g = Graphics.FromImage(ori))
            {
                foreach (KeyValuePair<double, Rectangle> p in rectDetected)
                {
                    g.FillRectangles(shadowBrush, new Rectangle[] { p.Value });
                    g.DrawString(p.Key.ToString("n4"), ff, br, new Point(p.Value.X + 1, p.Value.Y + 1));

                }
                g.Flush();
            }
            return ori;
        }

        private bool ComputeNormalizationUsingL2Norm(List<float> feature, float epsilon, bool needCheck)
        {
            float v_norm_square = 0;
            bool c = false;
            foreach (float v in feature)
            {
                v_norm_square += v * v;
            }
            float tempV = (float)Math.Sqrt(v_norm_square + epsilon);
            if (needCheck)
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] /= tempV;
                    if (feature[v] > 0.2F)
                    {
                        feature[v] = 0.2F;
                        c = true;
                    }
                }
            }
            else
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] /= tempV;
                }
            }
            return c;
        }

        private void ComputeNormalizationUsingL1Norm(List<float> feature, float epsilon)
        {
            float tempV = feature.Sum() + epsilon;
            for (int v = 0; v < feature.Count; v++)
            {
                feature[v] /= (float)tempV;
            }
        }

        private bool ComputeNormalizationUsingL2Norm(List<double> feature, double epsilon, bool needCheck)
        {
            double v_norm_square = 0;
            bool c = false;
            foreach (double v in feature)
            {
                v_norm_square += v * v;
            }
            double tempV = Math.Sqrt(v_norm_square + epsilon);
            if (needCheck)
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] /= tempV;
                    if (feature[v] > 0.2)
                    {
                        feature[v] = 0.2;
                        c = true;
                    }
                }
            }
            else
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] /= tempV;
                }
            }
            return c;
        }

        private void ComputeNormalizationUsingL1Norm(List<double> feature, double epsilon)
        {
            double tempV = feature.Sum() + epsilon;
            for (int v = 0; v < feature.Count; v++)
            {
                feature[v] /= tempV;
            }
        }

        private bool ComputeNormalizationUsingNewtonMethod(List<double> feature, double epsilon, bool needCheck)
        {
            double v_norm_square = 0;
            bool c = false;
            foreach (double v in feature)
            {
                v_norm_square += v * v;
            }
            double tempXf = v_norm_square + epsilon;
            double tempYnf = tempXf * 0.5 - 0x5f3759df;
            double tempYapproximatef = tempYnf * (3 - tempXf * tempYnf * tempYnf) / 2;
            if (needCheck)
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] *= tempYapproximatef;
                    if (feature[v] > 0.2)
                    {
                        feature[v] = 0.2;
                        c = true;
                    }
                }
            }
            else
            {
                for (int v = 0; v < feature.Count; v++)
                {
                    feature[v] *= tempYapproximatef;
                }
            }

            return c;
        }
    }
}
