using OpenCvSharp;
using System;

namespace VisionAlgolismViewer.Helpers
{
    public static class ImageEffects
    {
        #region Basic Adjustments

        /// <summary>
        /// Adjust brightness (-100 to 100)
        /// </summary>
        public static void AdjustBrightness(Mat image, float amount)
        {
            amount = Math.Clamp(amount, -100, 100);
            image.ConvertTo(image, -1, 1.0, amount * 2.55);
        }

        /// <summary>
        /// Adjust contrast (-100 to 100)
        /// </summary>
        public static void AdjustContrast(Mat image, float amount)
        {
            amount = Math.Clamp(amount, -100, 100);
            double factor = (100.0 + amount) / 100.0;
            image.ConvertTo(image, -1, factor, 0);
        }

        /// <summary>
        /// Adjust gamma (0.1 to 3.0)
        /// </summary>
        public static void AdjustGamma(Mat image, float gamma)
        {
            gamma = Math.Clamp(gamma, 0.1f, 3.0f);

            Mat lookupTable = new Mat(1, 256, MatType.CV_8U);
            unsafe
            {
                byte* p = (byte*)lookupTable.DataPointer;
                for (int i = 0; i < 256; i++)
                {
                    p[i] = (byte)Math.Clamp(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0, 0, 255);
                }
            }

            Cv2.LUT(image, lookupTable, image);
            lookupTable.Dispose();
        }

        #endregion

        #region Color Adjustments

        /// <summary>
        /// Adjust saturation (-100 to 100)
        /// </summary>
        public static void AdjustSaturation(Mat image, float amount)
        {
            amount = Math.Clamp(amount, -100, 100);
            float factor = 1.0f + (amount / 100.0f);

            Mat hsv = new Mat();
            Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

            Mat[] channels = Cv2.Split(hsv);
            channels[1].ConvertTo(channels[1], -1, factor, 0);
            channels[1] = channels[1].Threshold(255, 255, ThresholdTypes.Trunc);

            Cv2.Merge(channels, hsv);
            Cv2.CvtColor(hsv, image, ColorConversionCodes.HSV2BGR);

            foreach (var ch in channels) ch.Dispose();
            hsv.Dispose();
        }

        /// <summary>
        /// Adjust hue (-180 to 180 degrees)
        /// </summary>
        public static void AdjustHue(Mat image, float degrees)
        {
            degrees = Math.Clamp(degrees, -180, 180);
            float shift = degrees / 2.0f; // OpenCV uses 0-179 for Hue

            Mat hsv = new Mat();
            Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

            Mat[] channels = Cv2.Split(hsv);
            channels[0].ConvertTo(channels[0], -1, 1.0, shift);

            Cv2.Merge(channels, hsv);
            Cv2.CvtColor(hsv, image, ColorConversionCodes.HSV2BGR);

            foreach (var ch in channels) ch.Dispose();
            hsv.Dispose();
        }

        /// <summary>
        /// Adjust RGB channels individually (-100 to 100)
        /// </summary>
        public static void AdjustRGB(Mat image, float red, float green, float blue)
        {
            red = Math.Clamp(red, -100, 100) * 2.55f;
            green = Math.Clamp(green, -100, 100) * 2.55f;
            blue = Math.Clamp(blue, -100, 100) * 2.55f;

            Mat[] channels = Cv2.Split(image);

            channels[2].ConvertTo(channels[2], -1, 1.0, red);   // R
            channels[1].ConvertTo(channels[1], -1, 1.0, green); // G
            channels[0].ConvertTo(channels[0], -1, 1.0, blue);  // B

            Cv2.Merge(channels, image);
            foreach (var ch in channels) ch.Dispose();
        }

        #endregion

        #region Filters

        public static void ApplyGrayscale(Mat image)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(gray, image, ColorConversionCodes.GRAY2BGR);
            gray.Dispose();
        }

        public static void ApplySepia(Mat image)
        {
            Mat kernel = new Mat(3, 3, MatType.CV_32F);
            float[] sepiaData =
            {
                0.272f, 0.534f, 0.131f,
                0.349f, 0.686f, 0.168f,
                0.393f, 0.769f, 0.189f
            };
            kernel.SetArray(sepiaData);

            Cv2.Transform(image, image, kernel);
            kernel.Dispose();
        }

        public static void ApplyNegative(Mat image)
        {
            Cv2.BitwiseNot(image, image);
        }

        public static void ApplyBlur(Mat image, float sigma = 3f)
        {
            sigma = Math.Clamp(sigma, 0.1f, 50f);
            int ksize = ((int)(sigma * 3) * 2) + 1;
            Cv2.GaussianBlur(image, image, new Size(ksize, ksize), sigma);
        }

        public static void ApplySharpen(Mat image, float amount = 1.0f)
        {
            amount = Math.Clamp(amount, 0f, 5f);

            Mat blurred = new Mat();
            Cv2.GaussianBlur(image, blurred, new Size(0, 0), 3);
            Cv2.AddWeighted(image, 1.0 + amount, blurred, -amount, 0, image);
            blurred.Dispose();
        }

        public static void ApplyMedianFilter(Mat image, int radius = 2)
        {
            radius = Math.Clamp(radius, 1, 10);
            int ksize = (radius * 2) + 1;
            Cv2.MedianBlur(image, image, ksize);
        }

        #endregion

        #region Advanced Filters

        public static void ApplyEdgeDetection(Mat image)
        {
            Mat gray = new Mat();
            Mat edges = new Mat();

            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.Sobel(gray, edges, MatType.CV_8U, 1, 1);
            Cv2.CvtColor(edges, image, ColorConversionCodes.GRAY2BGR);

            gray.Dispose();
            edges.Dispose();
        }

        public static void ApplyEdgeDetectionLaplacian(Mat image)
        {
            Mat gray = new Mat();
            Mat edges = new Mat();
            Mat abs = new Mat();

            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.Laplacian(gray, edges, MatType.CV_16S, 3);
            Cv2.ConvertScaleAbs(edges, abs);
            Cv2.CvtColor(abs, image, ColorConversionCodes.GRAY2BGR);

            gray.Dispose();
            edges.Dispose();
            abs.Dispose();
        }

        public static void ApplyEmboss(Mat image)
        {
            Mat kernel = new Mat(3, 3, MatType.CV_32F);
            float[] emoboss=
            {
                -2, -1, 0,
                -1,  1, 1,
                 0,  1, 2
            };

            Mat result = new Mat();
            kernel.SetArray(emoboss);
            Cv2.Filter2D(image, result, -1, kernel);
            result.CopyTo(image);

            kernel.Dispose();
            result.Dispose();
        }

        public static void ApplyBoxBlur(Mat image, int radius = 3)
        {
            radius = Math.Clamp(radius, 1, 20);
            int ksize = (radius * 2) + 1;
            Cv2.BoxFilter(image, image, -1, new Size(ksize, ksize));
        }

        #endregion

        #region Eye Reduction

        /// <summary>
        /// Red-Eye Reduction - removes red eye effect from flash photography
        /// </summary>
        public static void ApplyRedEyeReduction(Mat image, int threshold = 150)
        {
            threshold = Math.Clamp(threshold, 100, 255);

            Mat[] channels = Cv2.Split(image);
            Mat mask = new Mat();

            // Detect red eyes: R > threshold && R > G * 1.5 && R > B * 1.5
            Mat redMask = channels[2].GreaterThan(threshold);
            Mat greenComp = channels[1] * 1.5;
            Mat blueComp = channels[0] * 1.5;
            Mat rgMask = channels[2].GreaterThan(greenComp);
            Mat rbMask = channels[2].GreaterThan(blueComp);

            Cv2.BitwiseAnd(redMask, rgMask, mask);
            Cv2.BitwiseAnd(mask, rbMask, mask);

            // Reduce red channel in detected areas
            channels[2].SetTo(channels[1], mask);

            Cv2.Merge(channels, image);

            foreach (var ch in channels) ch.Dispose();
            mask.Dispose();
            redMask.Dispose();
            greenComp.Dispose();
            blueComp.Dispose();
            rgMask.Dispose();
            rbMask.Dispose();
        }

        /// <summary>
        /// Green-Eye Reduction - removes green/yellow eye effect
        /// </summary>
        public static void ApplyGreenEyeReduction(Mat image, int threshold = 150)
        {
            threshold = Math.Clamp(threshold, 100, 255);

            Mat[] channels = Cv2.Split(image);
            Mat mask = new Mat();

            // Detect green eyes: G > threshold && G > R * 1.3 && G > B * 1.3
            Mat greenMask = channels[1].GreaterThan(threshold);
            Mat redComp = channels[2] * 1.3;
            Mat blueComp = channels[0] * 1.3;
            Mat grMask = channels[1].GreaterThan(redComp);
            Mat gbMask = channels[1].GreaterThan(blueComp);

            Cv2.BitwiseAnd(greenMask, grMask, mask);
            Cv2.BitwiseAnd(mask, gbMask, mask);

            // Reduce green channel in detected areas
            Mat avg = new Mat();
            Cv2.AddWeighted(channels[0], 0.5, channels[2], 0.5, 0, avg);
            avg.CopyTo(channels[1], mask);

            Cv2.Merge(channels, image);

            foreach (var ch in channels) ch.Dispose();
            mask.Dispose();
            greenMask.Dispose();
            redComp.Dispose();
            blueComp.Dispose();
            grMask.Dispose();
            gbMask.Dispose();
            avg.Dispose();
        }

        /// <summary>
        /// Blue-Eye Reduction - removes blue eye effect
        /// </summary>
        public static void ApplyBlueEyeReduction(Mat image, int threshold = 150)
        {
            threshold = Math.Clamp(threshold, 100, 255);

            Mat[] channels = Cv2.Split(image);
            Mat mask = new Mat();

            // Detect blue eyes: B > threshold && B > R * 1.3 && B > G * 1.3
            Mat blueMask = channels[0].GreaterThan(threshold);
            Mat redComp = channels[2] * 1.3;
            Mat greenComp = channels[1] * 1.3;
            Mat brMask = channels[0].GreaterThan(redComp);
            Mat bgMask = channels[0].GreaterThan(greenComp);

            Cv2.BitwiseAnd(blueMask, brMask, mask);
            Cv2.BitwiseAnd(mask, bgMask, mask);

            // Reduce blue channel in detected areas
            Mat avg = new Mat();
            Cv2.AddWeighted(channels[1], 0.5, channels[2], 0.5, 0, avg);
            avg.CopyTo(channels[0], mask);

            Cv2.Merge(channels, image);

            foreach (var ch in channels) ch.Dispose();
            mask.Dispose();
            blueMask.Dispose();
            redComp.Dispose();
            greenComp.Dispose();
            brMask.Dispose();
            bgMask.Dispose();
            avg.Dispose();
        }

        #endregion
    }
}
