using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisionAlgolismViewer.Models
{
    public class ImageProcessor : IDisposable
    {
        private Mat? _originalImage;
        private Mat? _processedImage;
        private bool _disposed = false;

        public void LoadImage(string filePath)
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();

            _originalImage = Cv2.ImRead(filePath, ImreadModes.Color);
            _processedImage = _originalImage.Clone();
        }

        public void LoadImage(BitmapSource source)
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();

            _originalImage = BitmapSourceToMat(source);
            _processedImage = _originalImage.Clone();
        }

        private Mat BitmapSourceToMat(BitmapSource source)
        {
            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgr24, null, 0);
            if (converted == null) return new Mat();

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 3;

            byte[] pixels = new byte[width * height];
            converted.CopyPixels(pixels, stride, 0);

            var mat = new Mat(height, width, MatType.CV_8UC3);
            Marshal.Copy(pixels, 0, mat.Data, pixels.Length);

            return mat;

        }

        public void ResetToOriginal()
        {
            if (_originalImage != null && !_originalImage.IsDisposed)
            {
                _processedImage?.Dispose();
                _processedImage = _originalImage.Clone();
            }
        }

        public BitmapImage? GetOriginalBitmap()
        {
            if (_originalImage == null || _originalImage.IsDisposed)
                return null;

            try
            {
                // OpenCV Mat to WPF BitmapImage - SUPER FAST!
                var bitmap = _originalImage.ToBitmap();

                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch
            {
                return null;
            }
        }

        public BitmapImage? GetProcessedBitmap()
        {
            if (_processedImage == null || _processedImage.IsDisposed)
                return null;

            try
            {
                // OpenCV Mat to WPF BitmapImage - SUPER FAST!
                var bitmap = _processedImage.ToBitmap();

                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch
            {
                return null;
            }
        }

        public Mat? GetProcessedImage() => _processedImage;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _originalImage?.Dispose();
                _processedImage?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
