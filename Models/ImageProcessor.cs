using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
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

        public void LoadImage(Stream stream)
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();

            stream.Position = 0;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var buffer = ms.ToArray();
                _originalImage = Cv2.ImDecode(buffer, ImreadModes.Color);
                _processedImage = _originalImage.Clone();
            }
        }

        public void ResetToOriginal()
        {
            if (_originalImage != null && !_originalImage.IsDisposed)
            {
                _processedImage?.Dispose();
                _processedImage = _originalImage.Clone();
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
