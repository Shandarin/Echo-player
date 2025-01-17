using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using LibVLCSharp.Shared;

public class WindowSizeHandler
{
    private const double MIN_WINDOW_WIDTH = 640;
    private const double MIN_WINDOW_HEIGHT = 480;
    private const double MAX_WINDOW_WIDTH = 1920;
    private const double MAX_WINDOW_HEIGHT = 1080;
    private const double ASPECT_RATIO_TOLERANCE = 0.01;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0; // 主屏幕宽度
    private const int SM_CYSCREEN = 1; // 主屏幕高度

    public (uint adjustedWidth, uint adjustedHeight, uint adjustedLeft, uint adjustedTop) CalculateWindowSize(
        uint currentMainWindowLeft,
        uint currentMainWindowTop,
        uint videoWidth,
        uint videoHeight,
        string ratio)
    {
        double aspectWidth, aspectHeight;

        // 判断是否为默认比例
        if (string.Equals(ratio, "Default", StringComparison.OrdinalIgnoreCase))
        {
            aspectWidth = videoWidth;
            aspectHeight = videoHeight;
        }
        else
        {
            var aspectParts = ratio.Split(':');
            if (aspectParts.Length != 2 ||
                !double.TryParse(aspectParts[0], out aspectWidth) ||
                !double.TryParse(aspectParts[1], out aspectHeight) ||
                aspectWidth <= 0 || aspectHeight <= 0)
            {
                Debug.WriteLine("Invalid aspect ratio: " + ratio);
                return CalculateWindowSize(videoWidth, videoHeight, currentMainWindowLeft, currentMainWindowTop,"default");
            }
            else
            {
                aspectHeight = videoHeight/(aspectWidth / aspectHeight);
                aspectWidth = videoWidth;
            }
        }

        

        // 获取屏幕缩放后的分辨率
        double scaledScreenWidth = SystemParameters.PrimaryScreenWidth;
        double scaledScreenHeight = SystemParameters.PrimaryScreenHeight;

       

        // 获取屏幕实际分辨率
        int actualScreenWidth = GetSystemMetrics(SM_CXSCREEN);
        int actualScreenHeight = GetSystemMetrics(SM_CYSCREEN);

        // 计算屏幕的缩放比例
        double dpiScaleX = scaledScreenWidth / actualScreenWidth;
        double dpiScaleY = scaledScreenHeight / actualScreenHeight;

        double scaleRatio = Math.Min(dpiScaleX, dpiScaleY);



        // 计算调整后的宽高
        double adjustedWidth = aspectWidth * scaleRatio;
        double adjustedHeight = aspectHeight * scaleRatio;

        //Debug.WriteLine($"adjustedWidth:{adjustedWidth}");

        uint adjustedLeft = currentMainWindowLeft;
        uint adjustedTop = currentMainWindowTop;

        // 确保窗口不会超出屏幕边界
        if (adjustedLeft + adjustedWidth > scaledScreenWidth)
        {
            adjustedLeft = (uint)(scaledScreenWidth - adjustedWidth);
        }

        if (adjustedTop + adjustedHeight > scaledScreenHeight)
        {
            adjustedTop = (uint)(scaledScreenHeight - adjustedHeight);
        }

        return ((uint)adjustedWidth, (uint)adjustedHeight, adjustedLeft, adjustedTop);
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