using System;
using System.Windows;
using LibVLCSharp.Shared;

public class WindowSizeHandler
{
    private const double MIN_WINDOW_WIDTH = 640;
    private const double MIN_WINDOW_HEIGHT = 480;
    private const double MAX_WINDOW_WIDTH = 1920;
    private const double MAX_WINDOW_HEIGHT = 1080;
    private const double ASPECT_RATIO_TOLERANCE = 0.01;

    public void AdjustWindowSize(Window window, MediaPlayer mediaPlayer)
    {
        if (mediaPlayer?.Media == null) return;

        // Get video dimensions
        uint videoWidth = mediaPlayer.Media.Tracks[0].Data.Video.Width;
        uint videoHeight = mediaPlayer.Media.Tracks[0].Data.Video.Height;

        if (videoWidth == 0 || videoHeight == 0) return;

        // Calculate target dimensions while maintaining aspect ratio
        double targetWidth = videoWidth;
        double targetHeight = videoHeight;

        // Scale down if larger than max dimensions
        if (targetWidth > MAX_WINDOW_WIDTH || targetHeight > MAX_WINDOW_HEIGHT)
        {
            double widthScale = MAX_WINDOW_WIDTH / targetWidth;
            double heightScale = MAX_WINDOW_HEIGHT / targetHeight;
            double scale = Math.Min(widthScale, heightScale);

            targetWidth *= scale;
            targetHeight *= scale;
        }

        // Scale up if smaller than min dimensions
        if (targetWidth < MIN_WINDOW_WIDTH || targetHeight < MIN_WINDOW_HEIGHT)
        {
            double widthScale = MIN_WINDOW_WIDTH / targetWidth;
            double heightScale = MIN_WINDOW_HEIGHT / targetHeight;
            double scale = Math.Max(widthScale, heightScale);

            targetWidth *= scale;
            targetHeight *= scale;
        }

        // Add space for menu bar and control bar
        targetHeight += 80; // Approximate height for UI elements

        // Set new window size
        window.Width = targetWidth;
        window.Height = targetHeight;

        // Center window on screen
        window.Left = (SystemParameters.PrimaryScreenWidth - window.Width) / 2;
        window.Top = (SystemParameters.PrimaryScreenHeight - window.Height) / 2;
    }

    public void ResetWindowSize(Window window)
    {
        // Reset to default dimensions
        window.Width = 1280;
        window.Height = 720;
        window.Left = (SystemParameters.PrimaryScreenWidth - window.Width) / 2;
        window.Top = (SystemParameters.PrimaryScreenHeight - window.Height) / 2;
    }
}