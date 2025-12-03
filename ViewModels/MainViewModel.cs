using DevExpress.Mvvm;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VisionAlgolismViewer.Helpers;
using VisionAlgolismViewer.Models;

namespace VisionAlgolismViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ImageProcessor _imageProcessor;
        private Timer? _debounceTimer;
        private CancellationTokenSource? _processingCts;
        private readonly object _processingLock = new object();
        private const int DebounceDelay = 300; // milliseconds

        #region Properties

        private BitmapImage? _currentImage;
        public BitmapImage? CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value, nameof(CurrentImage));
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value, nameof(StatusText));
        }

        private bool _isImageLoaded;
        public bool IsImageLoaded
        {
            get => _isImageLoaded;
            set => SetProperty(ref _isImageLoaded, value, nameof(IsImageLoaded));
        }

        // Basic Adjustments
        private float _brightness = 0;
        public float Brightness
        {
            get => _brightness;
            set
            {
                if (SetProperty(ref _brightness, value, nameof(Brightness)))
                {
                    DebounceApplyAdjustments(() => ApplyBasicAdjustments());
                }
            }
        }

        private float _contrast = 0;
        public float Contrast
        {
            get => _contrast;
            set
            {
                if (SetProperty(ref _contrast, value, nameof(Contrast)))
                {
                    DebounceApplyAdjustments(() => ApplyBasicAdjustments());
                }
            }
        }

        private float _gamma = 1.0f;
        public float Gamma
        {
            get => _gamma;
            set
            {
                if (SetProperty(ref _gamma, value, nameof(Gamma)))
                {
                    DebounceApplyAdjustments(() => ApplyBasicAdjustments());
                }
            }
        }

        // Color Adjustments
        private float _saturation = 0;
        public float Saturation
        {
            get => _saturation;
            set
            {
                if (SetProperty(ref _saturation, value, nameof(Saturation)))
                {
                    DebounceApplyAdjustments(() => ApplyColorAdjustments());
                }
            }
        }

        private float _hue = 0;
        public float Hue
        {
            get => _hue;
            set
            {
                if (SetProperty(ref _hue, value, nameof(Hue)))
                {
                    DebounceApplyAdjustments(() => ApplyColorAdjustments());
                }
            }
        }

        private float _redAdjustment = 0;
        public float RedAdjustment
        {
            get => _redAdjustment;
            set
            {
                if (SetProperty(ref _redAdjustment, value, nameof(RedAdjustment)))
                {
                    DebounceApplyAdjustments(() => ApplyColorAdjustments());
                }
            }
        }

        private float _greenAdjustment = 0;
        public float GreenAdjustment
        {
            get => _greenAdjustment;
            set
            {
                if (SetProperty(ref _greenAdjustment, value, nameof(GreenAdjustment)))
                {
                    DebounceApplyAdjustments(() => ApplyColorAdjustments());
                }
            }
        }

        private float _blueAdjustment = 0;
        public float BlueAdjustment
        {
            get => _blueAdjustment;
            set
            {
                if (SetProperty(ref _blueAdjustment, value, nameof(BlueAdjustment)))
                {
                    DebounceApplyAdjustments(() => ApplyColorAdjustments());
                }
            }
        }

        // Filter Parameters
        private float _blurSigma = 3.0f;
        public float BlurSigma
        {
            get => _blurSigma;
            set => SetProperty(ref _blurSigma, value, nameof(BlurSigma));
        }

        private float _sharpenAmount = 1.0f;
        public float SharpenAmount
        {
            get => _sharpenAmount;
            set => SetProperty(ref _sharpenAmount, value, nameof(SharpenAmount));
        }

        // Image Display
        private System.Windows.Media.Stretch _imageStretch = System.Windows.Media.Stretch.Uniform;
        public System.Windows.Media.Stretch ImageStretch
        {
            get => _imageStretch;
            set => SetProperty(ref _imageStretch, value, nameof(ImageStretch));
        }

        #endregion

        #region Commands

        public ICommand OpenImageCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand ResetImageCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand ActualSizeCommand { get; }
        public ICommand FillCommand { get; }
        public ICommand ApplyGrayscaleCommand { get; }
        public ICommand ApplySepiaCommand { get; }
        public ICommand ApplyNegativeCommand { get; }
        public ICommand ApplyBlurCommand { get; }
        public ICommand ApplySharpenCommand { get; }
        public ICommand ApplyMedianFilterCommand { get; }
        public ICommand ApplyEdgeDetectionCommand { get; }
        public ICommand ApplyEdgeDetectionLaplacianCommand { get; }
        public ICommand ApplyEmbossCommand { get; }
        public ICommand ApplyBoxBlurCommand { get; }
        public ICommand ApplyRedEyeReductionCommand { get; }
        public ICommand ApplyGreenEyeReductionCommand { get; }
        public ICommand ApplyBlueEyeReductionCommand { get; }

        #endregion

        public MainViewModel()
        {
            _imageProcessor = new ImageProcessor();

            // Initialize Commands
            OpenImageCommand = new DelegateCommand(OpenImage);
            SaveImageCommand = new DelegateCommand(SaveImage, CanSaveImage);
            ResetImageCommand = new DelegateCommand(ResetImage, CanResetImage);
            FitToWindowCommand = new DelegateCommand(FitToWindow);
            ActualSizeCommand = new DelegateCommand(ActualSize);
            FillCommand = new DelegateCommand(Fill);
            ApplyGrayscaleCommand = new DelegateCommand(ApplyGrayscale, CanApplyFilter);
            ApplySepiaCommand = new DelegateCommand(ApplySepia, CanApplyFilter);
            ApplyNegativeCommand = new DelegateCommand(ApplyNegative, CanApplyFilter);
            ApplyBlurCommand = new DelegateCommand(ApplyBlur, CanApplyFilter);
            ApplySharpenCommand = new DelegateCommand(ApplySharpen, CanApplyFilter);
            ApplyMedianFilterCommand = new DelegateCommand(ApplyMedianFilter, CanApplyFilter);
            ApplyEdgeDetectionCommand = new DelegateCommand(ApplyEdgeDetection, CanApplyFilter);
            ApplyEdgeDetectionLaplacianCommand = new DelegateCommand(ApplyEdgeDetectionLaplacian, CanApplyFilter);
            ApplyEmbossCommand = new DelegateCommand(ApplyEmboss, CanApplyFilter);
            ApplyBoxBlurCommand = new DelegateCommand(ApplyBoxBlur, CanApplyFilter);
        }

        #region Command Methods

        private void OpenImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|All Files|*.*",
                Title = "Open Image"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Trace.WriteLine("Image LoadStart");
                    _imageProcessor.LoadImage(dialog.FileName);
                    ResetAllParameters();
                    UpdateCurrentImage();
                    IsImageLoaded = true;
                    StatusText = $"Loaded: {Path.GetFileName(dialog.FileName)}";
                    RaiseCanExecuteChanged();
                    Trace.WriteLine("Image LoadEnd");
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                }
            }
        }

        private bool CanSaveImage() => IsImageLoaded;

        private void SaveImage()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp|All Files|*.*",
                Title = "Save Image"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var image = _imageProcessor.GetProcessedImage();
                    if (image != null)
                    {
                        image.SaveImage(dialog.FileName);
                        StatusText = $"Saved: {Path.GetFileName(dialog.FileName)}";
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                }
            }
        }

        private bool CanResetImage() => IsImageLoaded;

        private void ResetImage()
        {
            _imageProcessor.ResetToOriginal();
            ResetAllParameters();
            UpdateCurrentImage();
            StatusText = "Reset to original";
        }

        private void FitToWindow()
        {
            ImageStretch = System.Windows.Media.Stretch.Uniform;
            StatusText = "Fit to window";
        }

        private void ActualSize()
        {
            ImageStretch = System.Windows.Media.Stretch.None;
            StatusText = "Actual size (1:1)";
        }

        private void Fill()
        {
            ImageStretch = System.Windows.Media.Stretch.Fill;
            StatusText = "Fill window";
        }

        private bool CanApplyFilter() => IsImageLoaded;

        private void ApplyBasicAdjustments()
        {
            if (!IsImageLoaded) return;

            _imageProcessor.ResetToOriginal();
            var image = _imageProcessor.GetProcessedImage();
            if (image == null) return;

            if (Brightness != 0)
                ImageEffects.AdjustBrightness(image, Brightness);

            if (Contrast != 0)
                ImageEffects.AdjustContrast(image, Contrast);

            if (Math.Abs(Gamma - 1.0f) > 0.01f)
                ImageEffects.AdjustGamma(image, Gamma);
        }

        private void ApplyColorAdjustments()
        {
            if (!IsImageLoaded) return;

            _imageProcessor.ResetToOriginal();
            var image = _imageProcessor.GetProcessedImage();
            if (image == null) return;

            if (Saturation != 0)
                ImageEffects.AdjustSaturation(image, Saturation);

            if (Hue != 0)
                ImageEffects.AdjustHue(image, Hue);

            if (RedAdjustment != 0 || GreenAdjustment != 0 || BlueAdjustment != 0)
                ImageEffects.AdjustRGB(image, RedAdjustment, GreenAdjustment, BlueAdjustment);
        }

        private void ApplyGrayscale()
        {
            ApplyFilter(() => ImageEffects.ApplyGrayscale(_imageProcessor.GetProcessedImage()!), "Grayscale");
        }

        private void ApplySepia()
        {
            ApplyFilter(() => ImageEffects.ApplySepia(_imageProcessor.GetProcessedImage()!), "Sepia");
        }

        private void ApplyNegative()
        {
            ApplyFilter(() => ImageEffects.ApplyNegative(_imageProcessor.GetProcessedImage()!), "Negative");
        }

        private void ApplyBlur()
        {
            ApplyFilter(() => ImageEffects.ApplyBlur(_imageProcessor.GetProcessedImage()!, BlurSigma), "Blur");
        }

        private void ApplySharpen()
        {
            ApplyFilter(() => ImageEffects.ApplySharpen(_imageProcessor.GetProcessedImage()!, SharpenAmount), "Sharpen");
        }

        private void ApplyMedianFilter()
        {
            ApplyFilter(() => ImageEffects.ApplyMedianFilter(_imageProcessor.GetProcessedImage()!, 2), "Median Filter");
        }

        private void ApplyEdgeDetection()
        {
            ApplyFilter(() => ImageEffects.ApplyEdgeDetection(_imageProcessor.GetProcessedImage()!), "Edge Detection (Sobel)");
        }

        private void ApplyEdgeDetectionLaplacian()
        {
            ApplyFilter(() => ImageEffects.ApplyEdgeDetectionLaplacian(_imageProcessor.GetProcessedImage()!), "Edge Detection (Laplacian)");
        }

        private void ApplyEmboss()
        {
            ApplyFilter(() => ImageEffects.ApplyEmboss(_imageProcessor.GetProcessedImage()!), "Emboss");
        }

        private void ApplyBoxBlur()
        {
            ApplyFilter(() => ImageEffects.ApplyBoxBlur(_imageProcessor.GetProcessedImage()!, 3), "Box Blur");
        }

        private void ApplyFilter(Action filterAction, string filterName)
        {
            if (!IsImageLoaded) return;

            _imageProcessor.ResetToOriginal();
            filterAction();
            UpdateCurrentImage();
            StatusText = $"Applied: {filterName}";
        }

        #endregion

        #region Helper Methods

        private void DebounceApplyAdjustments(Action adjustmentAction)
        {
            if (!IsImageLoaded) return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                lock (_processingLock)
                {
                    _processingCts?.Cancel();
                    _processingCts?.Dispose();
                    _processingCts = new CancellationTokenSource();

                    var token = _processingCts.Token;

                    Task.Run(() =>
                    {
                        try
                        {
                            if (!token.IsCancellationRequested)
                            {
                                adjustmentAction();

                                if (!token.IsCancellationRequested)
                                {
                                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                                    {
                                        UpdateCurrentImage();
                                    });
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Ignore cancellation
                        }
                    }, token);
                }
            }, null, DebounceDelay, Timeout.Infinite);
        }

        private void UpdateCurrentImage()
        {
            CurrentImage = _imageProcessor.GetProcessedBitmap();
        }

        private void ResetAllParameters()
        {
            // Cancel any pending adjustments
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _processingCts?.Cancel();
            _processingCts?.Dispose();
            _processingCts = null;

            Brightness = 0;
            Contrast = 0;
            Gamma = 1.0f;
            Saturation = 0;
            Hue = 0;
            RedAdjustment = 0;
            GreenAdjustment = 0;
            BlueAdjustment = 0;
            BlurSigma = 3.0f;
            SharpenAmount = 1.0f;
        }

        private void RaiseCanExecuteChanged()
        {
            (SaveImageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ResetImageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyGrayscaleCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplySepiaCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyNegativeCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyBlurCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplySharpenCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyMedianFilterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyEdgeDetectionCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyEdgeDetectionLaplacianCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyEmbossCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ApplyBoxBlurCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
