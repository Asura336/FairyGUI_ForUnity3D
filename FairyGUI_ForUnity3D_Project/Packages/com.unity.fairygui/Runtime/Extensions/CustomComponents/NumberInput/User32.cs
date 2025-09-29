#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FairyGUI.Extensions
{
    using BOOL = Boolean;
    using DWORD = UInt32;
    using HMONITOR = IntPtr;
    using HWND = IntPtr;
    using LONG = Int32;


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public LONG x;
        public LONG y;

        public POINT(LONG x, LONG y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly void Deconstruct(out LONG x, out LONG y)
        {
            x = this.x;
            y = this.y;
        }

        public static implicit operator Vector2Int(in POINT p) => new Vector2Int(p.x, p.y);
        public static implicit operator Vector2(in POINT p) => new Vector2(p.x, p.y);
        public static implicit operator POINT(in Vector2Int p) => new POINT(p.x, p.y);
        public static implicit operator POINT(in Vector2 p) => new POINT((int)MathF.Round(p.x), (int)MathF.Round(p.y));

        public override readonly string ToString() => $"({x}, {y})";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public LONG left;
        public LONG top;
        public LONG right;
        public LONG bottom;

        public override readonly string ToString() => $"(left = {left}, right = {right}, top = {top}, bottom = {bottom})";

        public static implicit operator Rect(in RECT self) => new Rect
        {
            xMin = self.left,
            xMax = self.right,
            yMin = self.top,
            yMax = self.bottom
        };

        public static implicit operator RECT(in Rect rect) => new RECT
        {
            left = (int)rect.xMin,
            right = (int)rect.xMax,
            top = (int)rect.yMin,
            bottom = (int)rect.yMax
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        /// <summary>
        /// 标记结构体尺寸
        /// </summary>
        public DWORD size;
        public RECT rcMonitor;
        public RECT rcWork;
        /// <summary>
        /// 主显示器时值为 1
        /// </summary>
        public DWORD dwFlags;
    }

    enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL = 0,
        MONITOR_DEFAULTTOPRIMARY = 1,
        MONITOR_DEFAULTTONEAREST = 2
    }

    static class User32
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern BOOL GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern BOOL GetWindowRect(HWND hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern HWND GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern HMONITOR MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern BOOL SetCursorPos(LONG x, LONG y);


        /// <summary>
        /// 从指定光标位置计算光标所在的显示器范围，或者输出主显示器范围。
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="monitorRect"></param>
        /// <returns></returns>
        public static bool GetMonitorRectByCursorCoord(in POINT currentCoord, out RECT monitorRect)
        {
            MONITORINFO monitorInfo = default;
            monitorInfo.size = (uint)Marshal.SizeOf<MONITORINFO>();
            var monitor = MonitorFromPoint(currentCoord, MonitorOptions.MONITOR_DEFAULTTONULL);
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                monitorRect = monitorInfo.rcMonitor;
                return true;
            }
            else
            {
                GetWindowRect(GetDesktopWindow(), out monitorRect);
                return false;
            }
        }

        /// <summary>
        /// 传递指定窗体范围，如果当前光标超出范围则坐标按窗体范围取余，传出重新定位的光标位置和假想的光标原点偏移量。
        /// </summary>
        /// <param name="windowRect"></param>
        /// <param name="currentCoord"></param>
        /// <param name="originDeltaX"></param>
        /// <param name="originDeltaY"></param>
        public static void RoundCursor(in RECT windowRect, out POINT currentCoord, out int originDeltaX, out int originDeltaY)
        {
            GetCursorPos(out var p);
            CalculateRoundCursor(p, windowRect, out currentCoord, out originDeltaX, out originDeltaY);
            SetCursorPos(currentCoord.x, currentCoord.y);
        }

        /// <summary>
        /// 传递指定窗体范围和光标位置，如果当前光标靠近边缘则坐标按窗体范围取余，传出光标预期的位置和假想的光标原点偏移量，不会改变当前光标位置。
        /// </summary>
        /// <param name="currentCoord"></param>
        /// <param name="windowRect"></param>
        /// <param name="outCoord"></param>
        /// <param name="originDeltaX"></param>
        /// <param name="originDeltaY"></param>
        public static void CalculateRoundCursor(in POINT currentCoord, in RECT windowRect, out POINT outCoord, out int originDeltaX, out int originDeltaY)
        {
            const int edgeWidth = 12;
            const int twiceEdge = edgeWidth * 2 + 1;

            originDeltaX = originDeltaY = 0;

            outCoord = currentCoord;
            if (currentCoord.x > windowRect.right - edgeWidth)
            {
                originDeltaX = -(windowRect.right - windowRect.left - twiceEdge);
                outCoord.x = windowRect.left + edgeWidth + 1;
            }
            else if (currentCoord.x < windowRect.left + edgeWidth)
            {
                originDeltaX = windowRect.right - windowRect.left - twiceEdge;
                outCoord.x = windowRect.right - edgeWidth - 1;
            }
            if (currentCoord.y > windowRect.bottom - edgeWidth)
            {
                originDeltaY = -(windowRect.bottom - windowRect.top - twiceEdge);
                outCoord.y = windowRect.top + edgeWidth + 1;
            }
            else if (currentCoord.y < windowRect.top + edgeWidth)
            {
                originDeltaY = windowRect.bottom - windowRect.top - twiceEdge;
                outCoord.y = windowRect.bottom - edgeWidth - 1;
            }
        }
    }
}
#endif