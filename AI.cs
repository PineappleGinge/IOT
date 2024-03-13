using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TigerBot
{
    public class AI : IDisposable
    {
        private const int IMAGE_SIZE = 640;
        private const int NUM_DETECTIONS = 8400;
        
        public double minConfidence = 0.5;
        private Bitmap screenCaptureBitmap = null;
        private readonly RunOptions modeloptions;
        private InferenceSession onnxModel;
        private List<string> outputNames;

        private const int OFFSET = IMAGE_SIZE / 2;
        private Rectangle detectionBox = new Rectangle((Screen.PrimaryScreen.Bounds.Width / 2) - OFFSET / 2, (Screen.PrimaryScreen.Bounds.Height / 2) - OFFSET / 2, IMAGE_SIZE, IMAGE_SIZE);

        public AI(string modelPath)
        {
            modeloptions = new RunOptions();

            SessionOptions sessionOptions = new SessionOptions
            {
                EnableCpuMemArena = true,
                EnableMemoryPattern = true,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_PARALLEL
            };

            try
            {
                sessionOptions.AppendExecutionProvider_DML();
                onnxModel = new InferenceSession(modelPath, sessionOptions);
                outputNames = onnxModel.OutputMetadata.Keys.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load onnx model.\n{ex}");
                Environment.Exit(1);
            }
        }

        public int[] GetEnemy()
        {
            Bitmap frame = ScreenGrab();
            float[] inputArray = BitmapToFloatArray(frame);
            if (inputArray == null) { return null; }

            Tensor<float> inputTensor = new DenseTensor<float>(inputArray, new int[] { 1, 3, frame.Height, frame.Width });
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            var results = onnxModel.Run(inputs, outputNames, modeloptions);

            Tensor<float> outputTensor = results[0].AsTensor<float>();
            List<int> filteredIndices = Enumerable.Range(0, NUM_DETECTIONS).AsParallel().Where(i => outputTensor[0, 4, i] >= minConfidence).ToList();

            int[] closest = new int[3] { int.MaxValue, int.MaxValue, int.MaxValue };

            foreach (int i in filteredIndices)
            {
                float confidence = outputTensor[0, 4, i];

                float centerX = outputTensor[0, 0, i];
                float centerY = outputTensor[0, 1, i];
                float width = outputTensor[0, 2, i];
                float height = outputTensor[0, 3, i];

                int mouseX = (int)Math.Round(OFFSET / 2 - centerX);
                int mouseY = (int)Math.Round(OFFSET / 2 - centerY);

                int distance = (int)Math.Round(Math.Sqrt(Math.Pow(mouseX, 2) + Math.Pow(mouseY, 2)));

                if (distance < closest[2])
                    closest = new int[3] { -mouseX, -mouseY, distance };
            }

            if (closest[2] == int.MaxValue)
                return null;

            return closest;
        }

        public Bitmap ScreenGrab()
        {
            if (screenCaptureBitmap == null || screenCaptureBitmap.Width != detectionBox.Width || screenCaptureBitmap.Height != detectionBox.Height)
            {
                screenCaptureBitmap?.Dispose();
                screenCaptureBitmap = new Bitmap(detectionBox.Width, detectionBox.Height);
            }

            using (var g = Graphics.FromImage(screenCaptureBitmap))
            {
                g.CopyFromScreen(detectionBox.Left, detectionBox.Top, 0, 0, detectionBox.Size);
            }
            return screenCaptureBitmap;
        }

        public static float[] BitmapToFloatArray(Bitmap image)
        {
            int height = image.Height;
            int width = image.Width;
            float[] result = new float[3 * height * width];
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * height;
            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);
            for (int i = 0; i < rgbValues.Length / 3; i++)
            {
                int index = i * 3;
                result[i] = rgbValues[index + 2] / 255.0f;
                result[height * width + i] = rgbValues[index + 1] / 255.0f;
                result[2 * height * width + i] = rgbValues[index] / 255.0f;
            }

            image.UnlockBits(bmpData);

            return result;
        }

        public void Dispose()
        {
            onnxModel?.Dispose();
            screenCaptureBitmap?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
