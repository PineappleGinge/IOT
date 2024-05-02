using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TigerBotV2
{
    public class Ai : IDisposable
    {
        private const int ImageSize = 640;
        private const int NumDetections = 8400;
        
        private double _minConfidence = 0.05;
        private Bitmap _screenCaptureBitmap = null;
        private readonly RunOptions _modelOptions;
        private InferenceSession _onnxModel;
        private List<string> _outputNames;

        private const int Offset = ImageSize / 2;
        private Rectangle _detectionBox = new Rectangle((Screen.PrimaryScreen.Bounds.Width / 2) - Offset / 2, (Screen.PrimaryScreen.Bounds.Height / 2) - Offset / 2, ImageSize, ImageSize);

        public Ai(string modelPath)
        {
            _modelOptions = new RunOptions();
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
                _onnxModel = new InferenceSession(modelPath, sessionOptions);
                _outputNames = _onnxModel.OutputMetadata.Keys.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load onnx model.\n{ex}");
                Environment.Exit(1);
            }
        }

        public void SetMinConfidence(double confidence)
        {
            _minConfidence = confidence;
        }

        public int[] GetEnemy()
        {
            Bitmap frame = ScreenGrab();
            float[] inputArray = BitmapToFloatArray(frame);
            if (inputArray == null) { return null; }

            Tensor<float> inputTensor = new DenseTensor<float>(inputArray, new int[] { 1, 3, frame.Height, frame.Width });
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            var results = _onnxModel.Run(inputs, _outputNames, _modelOptions);

            Tensor<float> outputTensor = results[0].AsTensor<float>();
            List<int> filteredIndices = Enumerable.Range(0, NumDetections).AsParallel().Where(i => outputTensor[0, 4, i] >= _minConfidence).ToList();

            int[] closest = new int[3] { int.MaxValue, int.MaxValue, int.MaxValue };

            foreach (int i in filteredIndices)
            {
                float confidence = outputTensor[0, 4, i];

                float centerX = outputTensor[0, 0, i];
                float centerY = outputTensor[0, 1, i];
                float width = outputTensor[0, 2, i];
                float height = outputTensor[0, 3, i];

                int mouseX = (int)(Math.Round(Offset / 2 - centerX));
                int mouseY = (int)(Math.Round(Offset / 2 - centerY));

                int distance = (int)Math.Round(Math.Sqrt(Math.Pow(mouseX, 2) + Math.Pow(mouseY, 2)));

                if (distance < closest[2])
                    closest = new int[3] { -mouseX, -mouseY, distance };
            }

            if (closest[2] == int.MaxValue)
                return null;

            return closest;
        }

        public List<int[]> GetEnemys()
        {
            Bitmap frame = ScreenGrab();
            float[] inputArray = BitmapToFloatArray(frame);
            if (inputArray == null) { return null; }

            Tensor<float> inputTensor = new DenseTensor<float>(inputArray, new int[] { 1, 3, frame.Height, frame.Width });
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            var results = _onnxModel.Run(inputs, _outputNames, _modelOptions);

            Tensor<float> outputTensor = results[0].AsTensor<float>();
            List<int> filteredIndices = Enumerable.Range(0, NumDetections).AsParallel().Where(i => outputTensor[0, 4, i] >= _minConfidence).ToList();
            List<int[]> players = new List<int[]>();

            foreach (int i in filteredIndices)
            {
                float confidence = outputTensor[0, 4, i];

                float centerX = outputTensor[0, 0, i];
                float centerY = outputTensor[0, 1, i];
                float width = outputTensor[0, 2, i];
                float height = outputTensor[0, 3, i];

                int mouseX = (int)(Math.Round(Offset / 2 - centerX));
                int mouseY = (int)(Math.Round(Offset / 2 - centerY));

                int distance = (int)Math.Round(Math.Sqrt(Math.Pow(mouseX, 2) + Math.Pow(mouseY, 2)));

                players.Add(new int[5] { (int)(-mouseX - Math.Round(width / 2)), (int)(-mouseY - Math.Round(height / 2)), (int)Math.Round(width), (int)Math.Round(width), distance });
            }

            if (players.Count() < 1)
                return null;

            return players;
        }

        public Bitmap ScreenGrab()
        {
            if (_screenCaptureBitmap == null || _screenCaptureBitmap.Width != _detectionBox.Width || _screenCaptureBitmap.Height != _detectionBox.Height)
            {
                _screenCaptureBitmap?.Dispose();
                _screenCaptureBitmap = new Bitmap(_detectionBox.Width, _detectionBox.Height);
            }

            using (var g = Graphics.FromImage(_screenCaptureBitmap))
            {
                g.CopyFromScreen(_detectionBox.Left, _detectionBox.Top, 0, 0, _detectionBox.Size);
            }
            return _screenCaptureBitmap;
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
            _onnxModel?.Dispose();
            _screenCaptureBitmap?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
