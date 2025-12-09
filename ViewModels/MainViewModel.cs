using DevExpress.Mvvm;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VisionAlgolismViewer.Helpers;
using VisionAlgolismViewer.Models;

namespace VisionAlgolismViewer.ViewModels
{
    public enum EffectType
    {
        None,
        BasicAdjustments,
        ColorAdjustments,
        Grayscale,
        Sepia,
        Negative,
        Blur,
        Sharpen,
        MedianFilter,
        EdgeDetection,
        EdgeDetectionLaplacian,
        Emboss,
        BoxBlur,
        RedEyeReduction,
        GreenEyeReduction,
        BlueEyeReduction
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly ImageProcessor _imageProcessor;
        private Timer? _debounceTimer;
        private CancellationTokenSource? _processingCts;
        private readonly object _processingLock = new object();
        private const int DebounceDelay = 300; // milliseconds

        #region Properties

        private BitmapImage? _originalImage;
        public BitmapImage? OriginalImage
        {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value, nameof(OriginalImage));
        }

        private BitmapImage? _processedImage;
        public BitmapImage? ProcessedImage
        {
            get => _processedImage;
            set => SetProperty(ref _processedImage, value, nameof(ProcessedImage));
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

        private EffectType _currentEffectType = EffectType.None;
        public EffectType CurrentEffectType
        {
            get => _currentEffectType;
            set => SetProperty(ref _currentEffectType, value, nameof(CurrentEffectType));
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
            set
            {
                if (SetProperty(ref _blurSigma, value, nameof(BlurSigma)))
                {
                    if (CurrentEffectType == EffectType.Blur)
                    {
                        DebounceApplyAdjustments(() => ApplyBlur());
                    }
                }
            }
        }

        private float _sharpenAmount = 1.0f;
        public float SharpenAmount
        {
            get => _sharpenAmount;
            set
            {
                if (SetProperty(ref _sharpenAmount, value, nameof(SharpenAmount)))
                {
                    if (CurrentEffectType == EffectType.Sharpen)
                    {
                        DebounceApplyAdjustments(() => ApplySharpen());
                    }
                }
            }
        }

        private int _medianFilterRadius = 2;
        public int MedianFilterRadius
        {
            get => _medianFilterRadius;
            set
            {
                if (SetProperty(ref _medianFilterRadius, value, nameof(MedianFilterRadius)))
                {
                    if (CurrentEffectType == EffectType.MedianFilter)
                    {
                        DebounceApplyAdjustments(() => ApplyMedianFilter());
                    }
                }
            }
        }

        private int _boxBlurRadius = 3;
        public int BoxBlurRadius
        {
            get => _boxBlurRadius;
            set
            {
                if (SetProperty(ref _boxBlurRadius, value, nameof(BoxBlurRadius)))
                {
                    if (CurrentEffectType == EffectType.BoxBlur)
                    {
                        DebounceApplyAdjustments(() => ApplyBoxBlur());
                    }
                }
            }
        }

        // Eye Reduction Parameters
        // Red Eye Reduction
        private int _redEyeThreshold = 150;
        public int RedEyeThreshold
        {
            get => _redEyeThreshold;
            set
            {
                if (SetProperty(ref _redEyeThreshold, value, nameof(RedEyeThreshold)))
                {
                    if (CurrentEffectType == EffectType.RedEyeReduction)
                    {
                        ApplyRedReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        private int _redEyeLevel = 50;
        public int RedEyeLevel
        {
            get => _redEyeLevel;
            set
            {
                if (SetProperty(ref _redEyeLevel, value, nameof(RedEyeLevel)))
                {
                    if (CurrentEffectType == EffectType.RedEyeReduction)
                    {
                        ApplyRedReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        // Green Eye Reduction
        private int _greenEyeThreshold = 150;
        public int GreenEyeThreshold
        {
            get => _greenEyeThreshold;
            set
            {
                if (SetProperty(ref _greenEyeThreshold, value, nameof(GreenEyeThreshold)))
                {
                    if (CurrentEffectType == EffectType.GreenEyeReduction)
                    {
                        ApplyGreenReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        private int _greenEyeLevel = 50;
        public int GreenEyeLevel
        {
            get => _greenEyeLevel;
            set
            {
                if (SetProperty(ref _greenEyeLevel, value, nameof(GreenEyeLevel)))
                {
                    if (CurrentEffectType == EffectType.GreenEyeReduction)
                    {
                        ApplyGreenReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        // Blue Eye Reduction
        private int _blueEyeThreshold = 150;
        public int BlueEyeThreshold
        {
            get => _blueEyeThreshold;
            set
            {
                if (SetProperty(ref _blueEyeThreshold, value, nameof(BlueEyeThreshold)))
                {
                    if (CurrentEffectType == EffectType.BlueEyeReduction)
                    {
                        ApplyBlueReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        private int _blueEyeLevel = 50;
        public int BlueEyeLevel
        {
            get => _blueEyeLevel;
            set
            {
                if (SetProperty(ref _blueEyeLevel, value, nameof(BlueEyeLevel)))
                {
                    if (CurrentEffectType == EffectType.BlueEyeReduction)
                    {
                        ApplyBlueReductionInternal();
                        UpdateCurrentImage();
                    }
                }
            }
        }

        // Image Display
        private System.Windows.Media.Stretch _imageStretch = System.Windows.Media.Stretch.Uniform;
        public System.Windows.Media.Stretch ImageStretch
        {
            get => _imageStretch;
            set => SetProperty(ref _imageStretch, value, nameof(ImageStretch));
        }

        // Image History Management
        private ObservableCollection<BitmapImage> _imageHistory = new ObservableCollection<BitmapImage>();
        public ObservableCollection<BitmapImage> ImageHistory
        {
            get => _imageHistory;
            set => SetProperty(ref _imageHistory, value, nameof(ImageHistory));
        }

        private int _currentHistoryIndex = -1;
        public int CurrentHistoryIndex
        {
            get => _currentHistoryIndex;
            set
            {
                if (SetProperty(ref _currentHistoryIndex, value, nameof(CurrentHistoryIndex)))
                {
                    RaisePropertyChanged(nameof(CanNavigateBack));
                    RaisePropertyChanged(nameof(CanNavigateForward));
                    RaisePropertyChanged(nameof(HistoryStatusText));
                }
            }
        }

        public bool CanNavigateBack => CurrentHistoryIndex > 0;
        public bool CanNavigateForward => CurrentHistoryIndex < ImageHistory.Count - 1;

        public string HistoryStatusText => $"History: {CurrentHistoryIndex + 1} / {ImageHistory.Count}";

        public int HistoryCount => ImageHistory.Count;

        private const int MaxHistoryCount = 5;

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
        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateForwardCommand { get; }
        public ICommand ApplyCurrentChangesCommand { get; }
        public ICommand ClearPreviewCommand { get; }

        #endregion

        public MainViewModel()
        {
            _imageProcessor = new ImageProcessor();

            // Subscribe to ImageHistory collection changes
            ImageHistory.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(HistoryStatusText));
                RaisePropertyChanged(nameof(HistoryCount));
                RaisePropertyChanged(nameof(CanNavigateBack));
                RaisePropertyChanged(nameof(CanNavigateForward));
            };

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
            ApplyRedEyeReductionCommand = new DelegateCommand(ApplyRedReduction, CanApplyFilter);
            ApplyBlueEyeReductionCommand = new DelegateCommand(ApplyBlueReduction, CanApplyFilter);
            ApplyGreenEyeReductionCommand = new DelegateCommand(ApplyGreenReduction, CanApplyFilter);
            NavigateBackCommand = new DelegateCommand(NavigateBack, () => CanNavigateBack);
            NavigateForwardCommand = new DelegateCommand(NavigateForward, () => CanNavigateForward);
            ApplyCurrentChangesCommand = new DelegateCommand(ApplyCurrentChanges, CanApplyCurrentChanges);
            ClearPreviewCommand = new DelegateCommand(ClearHistory, CanClearPreview);
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
                    UpdateOriginalImage();
                    UpdateCurrentImage();
                    ResetHistory();

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

        private void ResetHistory()
        {
            // Initialize history with original image
            ImageHistory.Clear();
            if (OriginalImage != null)
            {
                ImageHistory.Add(OriginalImage);
                CurrentHistoryIndex = 0;
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
            CurrentEffectType = EffectType.Grayscale;
            ApplyFilter(() => ImageEffects.ApplyGrayscale(_imageProcessor.GetProcessedImage()!), "Grayscale");
        }

        private void ApplySepia()
        {
            CurrentEffectType = EffectType.Sepia;
            ApplyFilter(() => ImageEffects.ApplySepia(_imageProcessor.GetProcessedImage()!), "Sepia");
        }

        private void ApplyNegative()
        {
            CurrentEffectType = EffectType.Negative;
            ApplyFilter(() => ImageEffects.ApplyNegative(_imageProcessor.GetProcessedImage()!), "Negative");
        }

        private void ApplyBlur()
        {
            CurrentEffectType = EffectType.Blur;
            ApplyFilter(() => ImageEffects.ApplyBlur(_imageProcessor.GetProcessedImage()!, BlurSigma), "Blur");
        }

        private void ApplySharpen()
        {
            CurrentEffectType = EffectType.Sharpen;
            ApplyFilter(() => ImageEffects.ApplySharpen(_imageProcessor.GetProcessedImage()!, SharpenAmount), "Sharpen");
        }

        private void ApplyMedianFilter()
        {
            CurrentEffectType = EffectType.MedianFilter;
            ApplyFilter(() => ImageEffects.ApplyMedianFilter(_imageProcessor.GetProcessedImage()!, MedianFilterRadius), "Median Filter");
        }

        private void ApplyEdgeDetection()
        {
            CurrentEffectType = EffectType.EdgeDetection;
            ApplyFilter(() => ImageEffects.ApplyEdgeDetection(_imageProcessor.GetProcessedImage()!), "Edge Detection (Sobel)");
        }

        private void ApplyEdgeDetectionLaplacian()
        {
            CurrentEffectType = EffectType.EdgeDetectionLaplacian;
            ApplyFilter(() => ImageEffects.ApplyEdgeDetectionLaplacian(_imageProcessor.GetProcessedImage()!), "Edge Detection (Laplacian)");
        }

        private void ApplyEmboss()
        {
            CurrentEffectType = EffectType.Emboss;
            ApplyFilter(() => ImageEffects.ApplyEmboss(_imageProcessor.GetProcessedImage()!), "Emboss");
        }

        private void ApplyBoxBlur()
        {
            CurrentEffectType = EffectType.BoxBlur;
            ApplyFilter(() => ImageEffects.ApplyBoxBlur(_imageProcessor.GetProcessedImage()!, BoxBlurRadius), "Box Blur");
        }

        private void ApplyRedReduction()
        {
            if (!IsImageLoaded) return;

            CurrentEffectType = EffectType.RedEyeReduction;
            ApplyRedReductionInternal();
            UpdateCurrentImage();
            StatusText = $"Red Reduction - Threshold: {RedEyeThreshold}, Level: {RedEyeLevel}";
        }

        private void ApplyRedReductionInternal()
        {
            if (!IsImageLoaded) return;
            // Don't reset - accumulate effects
            ImageEffects.ApplyRedEyeReduction(_imageProcessor.GetProcessedImage()!, RedEyeThreshold, RedEyeLevel);
        }

        private void ApplyGreenReduction()
        {
            if (!IsImageLoaded) return;

            CurrentEffectType = EffectType.GreenEyeReduction;
            ApplyGreenReductionInternal();
            UpdateCurrentImage();
            StatusText = $"Green Reduction - Threshold: {GreenEyeThreshold}, Level: {GreenEyeLevel}";
        }

        private void ApplyGreenReductionInternal()
        {
            if (!IsImageLoaded) return;
            // Don't reset - accumulate effects
            ImageEffects.ApplyGreenEyeReduction(_imageProcessor.GetProcessedImage()!, GreenEyeThreshold, GreenEyeLevel);
        }

        private void ApplyBlueReduction()
        {
            if (!IsImageLoaded) return;

            CurrentEffectType = EffectType.BlueEyeReduction;
            ApplyBlueReductionInternal();
            UpdateCurrentImage();
            StatusText = $"Blue Reduction - Threshold: {BlueEyeThreshold}, Level: {BlueEyeLevel}";
        }

        private void ApplyBlueReductionInternal()
        {
            if (!IsImageLoaded) return;
            // Don't reset - accumulate effects
            ImageEffects.ApplyBlueEyeReduction(_imageProcessor.GetProcessedImage()!, BlueEyeThreshold, BlueEyeLevel);
        }

        private void ApplyFilter(Action filterAction, string filterName)
        {
            if (!IsImageLoaded) return;

            // Don't reset - accumulate effects
            filterAction();
            UpdateCurrentImage();
            StatusText = $"Applied: {filterName}";
        }

        private void NavigateBack()
        {
            if (!CanNavigateBack) return;

            CurrentHistoryIndex--;
            ProcessedImage = ImageHistory[CurrentHistoryIndex];

            // Update ImageProcessor with the current history image
            UpdateProcessorFromHistory();

            StatusText = "Navigated to previous image";
            RaiseNavigationChanged();
        }

        private void NavigateForward()
        {
            if (!CanNavigateForward) return;

            CurrentHistoryIndex++;
            ProcessedImage = ImageHistory[CurrentHistoryIndex];

            // Update ImageProcessor with the current history image
            UpdateProcessorFromHistory();

            StatusText = "Navigated to next image";
            RaiseNavigationChanged();
        }

        private bool CanApplyCurrentChanges() => IsImageLoaded;

        private void ApplyCurrentChanges()
        {
            if (!IsImageLoaded || ProcessedImage == null) return;

            AddToHistory(ProcessedImage);
            StatusText = "Changes applied to history";
        }

        private bool CanClearPreview() => IsImageLoaded && CurrentHistoryIndex >= 0;

        private void ClearHistory()
        {
            if (!IsImageLoaded) return;

            _imageProcessor.CopyOrginImage();
            ResetHistory();
            UpdateCurrentImage();
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
            ProcessedImage = _imageProcessor.GetProcessedBitmap();
        }

        private void UpdateOriginalImage()
        {
            OriginalImage = _imageProcessor.GetOriginalBitmap();
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
            (ApplyCurrentChangesCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ClearPreviewCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void RaiseNavigationChanged()
        {
            (NavigateBackCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (NavigateForwardCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void AddToHistory(BitmapImage? image)
        {
            if (image == null) return;

            // If we're not at the end of history, remove everything after current position
            if (CurrentHistoryIndex < ImageHistory.Count - 1)
            {
                for (int i = ImageHistory.Count - 1; i > CurrentHistoryIndex; i--)
                {
                    ImageHistory.RemoveAt(i);
                }
            }

            // Add new image to history
            ImageHistory.Add(image);

            // Enforce max history count - remove oldest (but keep the original at index 0)
            while (ImageHistory.Count > MaxHistoryCount)
            {
                ImageHistory.RemoveAt(1); // Remove second oldest, keep original
            }

            // Update index to point to the new image
            CurrentHistoryIndex = ImageHistory.Count - 1;
            RaisePropertyChanged(nameof(HistoryStatusText));
            RaiseNavigationChanged();
        }

        private void UpdateProcessorFromHistory()
        {
            // Convert BitmapImage back to Mat and update processor
            if (ProcessedImage != null)
            {
                _imageProcessor.LoadImage(ProcessedImage);
            }
        }

        #endregion
    }
}
