using DevExpress.Xpf.Core;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VisionAlgolismViewer.ViewModels;

namespace VisionAlgolismViewer
{
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateImageDisplay();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ImageStretch))
            {
                UpdateImageDisplay();
            }
        }

        private void UpdateImageDisplay()
        {
            if (DataContext is MainViewModel viewModel)
            {
                // Actual Size (None) uses ScrollViewer, others use direct Image
                if (viewModel.ImageStretch == System.Windows.Media.Stretch.None)
                {
                    MainImage.Visibility = Visibility.Collapsed;
                    ImageScrollViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    MainImage.Visibility = Visibility.Visible;
                    ImageScrollViewer.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePixelInfo(MainImage, e);
        }

        private void MainImage_MouseLeave(object sender, MouseEventArgs e)
        {
            PixelInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ScrollImage_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePixelInfo(ScrollImage, e);
        }

        private void ScrollImage_MouseLeave(object sender, MouseEventArgs e)
        {
            PixelInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdatePixelInfo(Image image, MouseEventArgs e)
        {
            if (image.Source is not BitmapSource bitmapSource)
            {
                PixelInfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Get mouse position relative to image
            var position = e.GetPosition(image);

            // Calculate actual pixel coordinates
            var imageWidth = bitmapSource.PixelWidth;
            var imageHeight = bitmapSource.PixelHeight;

            int x = (int)(position.X / image.ActualWidth * imageWidth);
            int y = (int)(position.Y / image.ActualHeight * imageHeight);

            // Check bounds
            if (x < 0 || x >= imageWidth || y < 0 || y >= imageHeight)
            {
                PixelInfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                // Get pixel color
                var color = GetPixelColor(bitmapSource, x, y);

                // Convert to HSV
                var (h, s, v) = RgbToHsv(color.R, color.G, color.B);

                // Update UI
                PixelPositionText.Text = $"X: {x}, Y: {y}";
                PixelRgbText.Text = $"RGB: ({color.R}, {color.G}, {color.B})";
                PixelHsvText.Text = $"HSV: ({h:F0}°, {s:F0}%, {v:F0}%)";

                PixelInfoPanel.Visibility = Visibility.Visible;
            }
            catch
            {
                PixelInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        private Color GetPixelColor(BitmapSource bitmap, int x, int y)
        {
            if (bitmap.Format != PixelFormats.Bgra32 && bitmap.Format != PixelFormats.Bgr32)
            {
                // Convert to Bgra32 if needed
                var convertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                bitmap = convertedBitmap;
            }

            var bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            var stride = bitmap.PixelWidth * bytesPerPixel;
            var pixels = new byte[bytesPerPixel];

            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, stride, 0);

            return Color.FromArgb(
                bytesPerPixel == 4 ? pixels[3] : (byte)255,
                pixels[2], // R
                pixels[1], // G
                pixels[0]  // B
            );
        }

        private (double h, double s, double v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            // Hue
            double h = 0;
            if (delta != 0)
            {
                if (max == rd)
                    h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd)
                    h = 60 * (((bd - rd) / delta) + 2);
                else
                    h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;

            // Saturation
            double s = (max == 0) ? 0 : (delta / max) * 100;

            // Value
            double v = max * 100;

            return (h, s, v);
        }
    }
}
