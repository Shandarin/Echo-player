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

    public (uint adjustedWidth, uint adjustedHeight) CalculateFullWindowSize(
        uint videoWidth,
        uint videoHeight,
        string ratio)
    {
        // 1) 解析宽高比
        double aspectWidth, aspectHeight;
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
                aspectWidth = videoWidth;
                aspectHeight = videoHeight;
            }
            else
            {
                aspectHeight = videoHeight / (aspectWidth / aspectHeight);
                aspectWidth = videoWidth;
            }
        }

        // 2) 获取屏幕 DPI 缩放后的分辨率
        double scaledScreenWidth = SystemParameters.PrimaryScreenWidth;
        double scaledScreenHeight = SystemParameters.PrimaryScreenHeight;

        // 如果需要获取原始像素分辨率(不含DPI缩放)，可用:
        // int actualScreenWidth = GetSystemMetrics(SM_CXSCREEN);
        // int actualScreenHeight = GetSystemMetrics(SM_CYSCREEN);
        // double dpiScaleX = scaledScreenWidth / actualScreenWidth;
        // double dpiScaleY = scaledScreenHeight / actualScreenHeight;
        // double scaleRatio = Math.Min(dpiScaleX, dpiScaleY);

        // 先把视频“原尺寸”转换到当前 DPI 下
        //（如果你坚持做和原方法相同的计算，可继续用前述 scaleRatio。此处演示直接用 scaledScreenWidth/scaledScreenHeight）
        double scaledAspectWidth = aspectWidth;
        double scaledAspectHeight = aspectHeight;
        // 如果你原本想对 aspectWidth/Height 做 DPI 缩放，可用:
        // scaledAspectWidth = aspectWidth * scaleRatio;
        // scaledAspectHeight = aspectHeight * scaleRatio;

        // 3) 让视频适配屏幕 (缩放以适应屏幕宽高最短的一边)
        //   计算能让视频完整显示在屏幕内的最大拉伸倍数
        double ratioToFillScreen = Math.Min(
            scaledScreenWidth / scaledAspectWidth,
            scaledScreenHeight / scaledAspectHeight
        );

        double finalWidth = scaledAspectWidth * ratioToFillScreen;
        double finalHeight = scaledAspectHeight * ratioToFillScreen;

        // 4) 让其居中
        double left = (scaledScreenWidth - finalWidth) / 2.0;
        double top = (scaledScreenHeight - finalHeight) / 2.0;

        // 5) 返回整型
        return (
            (uint)finalWidth,
            (uint)finalHeight
        );
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