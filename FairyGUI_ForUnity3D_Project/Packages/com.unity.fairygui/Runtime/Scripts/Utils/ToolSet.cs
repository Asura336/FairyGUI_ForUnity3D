using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class ToolSet
    {
        internal const float PI_HALF = Mathf.PI * 0.5f;
        internal const float PI_DOUBLE = Mathf.PI * 2;
        internal const float PI_DIV_4 = Mathf.PI / 4f;
        internal const float PI_DIV_8 = Mathf.PI / 8f;

        internal static float Pow2(float val) => val * val;

        public static Color ConvertFromHtmlColor(string str)
        {
            if (str.Length < 7 || str[0] != '#')
                return Color.black;

            if (str.Length == 9)
            {
                //optimize:avoid using Convert.ToByte and Substring
                //return new Color32(Convert.ToByte(str.Substring(3, 2), 16), Convert.ToByte(str.Substring(5, 2), 16),
                //  Convert.ToByte(str.Substring(7, 2), 16), Convert.ToByte(str.Substring(1, 2), 16));

                return new Color32((byte)(CharToHex(str[3]) * 16 + CharToHex(str[4])),
                    (byte)(CharToHex(str[5]) * 16 + CharToHex(str[6])),
                    (byte)(CharToHex(str[7]) * 16 + CharToHex(str[8])),
                    (byte)(CharToHex(str[1]) * 16 + CharToHex(str[2])));
            }
            else
            {
                //return new Color32(Convert.ToByte(str.Substring(1, 2), 16), Convert.ToByte(str.Substring(3, 2), 16),
                //Convert.ToByte(str.Substring(5, 2), 16), 255);

                return new Color32((byte)(CharToHex(str[1]) * 16 + CharToHex(str[2])),
                    (byte)(CharToHex(str[3]) * 16 + CharToHex(str[4])),
                    (byte)(CharToHex(str[5]) * 16 + CharToHex(str[6])),
                    255);
            }
        }

        public static Color ColorFromRGB(int value)
        {
            return new Color(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f, 1);
        }

        public static Color ColorFromRGBA(uint value)
        {
            return new Color(((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f, ((value >> 24) & 0xFF) / 255f);
        }

        public static int CharToHex(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)c - 48;
            if (c >= 'A' && c <= 'F')
                return 10 + (int)c - 65;
            else if (c >= 'a' && c <= 'f')
                return 10 + (int)c - 97;
            else
                return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Dot(this in Vector3 lhs, in Vector3 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }

        internal static int IndexOf(this StringBuilder sb, char c, int start = 0)
        {
            if (sb == null || sb.Length == 0) { return -1; }
            int len = sb.Length;
            for (int i = start; i < len; i++)
            {
                if (sb[i] == c) { return i; }
            }
            return -1;
        }

        public static Rect Intersection(ref Rect rect1, ref Rect rect2)
        {
            if (rect1.width == 0 || rect1.height == 0 || rect2.width == 0 || rect2.height == 0)
                return new Rect(0, 0, 0, 0);

            float left = rect1.xMin > rect2.xMin ? rect1.xMin : rect2.xMin;
            float right = rect1.xMax < rect2.xMax ? rect1.xMax : rect2.xMax;
            float top = rect1.yMin > rect2.yMin ? rect1.yMin : rect2.yMin;
            float bottom = rect1.yMax < rect2.yMax ? rect1.yMax : rect2.yMax;

            if (left > right || top > bottom)
                return new Rect(0, 0, 0, 0);
            else
                return Rect.MinMaxRect(left, top, right, bottom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Plus3(this ref Vector3 a, in Vector3 b)
        {
            a.x += b.x;
            a.y += b.y;
            a.z += b.z;
        }

        /// <summary>
        /// same as <see cref="Matrix4x4"/>'s * operator, faster
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Mul(this in Matrix4x4 lhs, in Matrix4x4 rhs, ref Matrix4x4 result)
        {
            result.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            result.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            result.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            result.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;
            result.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            result.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            result.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            result.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;
            result.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            result.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            result.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            result.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;
            result.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            result.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            result.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            result.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;
        }

        /// <summary>
        /// same as <see cref="Matrix4x4.MultiplyPoint(Vector3)"/>, faster
        /// </summary>
        /// <param name="mul"></param>
        /// <param name="point"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MultiplyPoint(this in Matrix4x4 mul, in Vector3 point, ref Vector3 result)
        {
            result.x = mul.m00 * point.x + mul.m01 * point.y + mul.m02 * point.z + mul.m03;
            result.y = mul.m10 * point.x + mul.m11 * point.y + mul.m12 * point.z + mul.m13;
            result.z = mul.m20 * point.x + mul.m21 * point.y + mul.m22 * point.z + mul.m23;
            float num = mul.m30 * point.x + mul.m31 * point.y + mul.m32 * point.z + mul.m33;
            num = 1f / num;
            result.x *= num;
            result.y *= num;
            result.z *= num;
        }

        /// <summary>
        /// same as <see cref="Matrix4x4.MultiplyPoint3x4(Vector3)"/>, faster
        /// </summary>
        /// <param name="mul"></param>
        /// <param name="point"></param>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MultiplyPoint3x4(this in Matrix4x4 mul, in Vector3 point, ref Vector3 result)
        {
            result.x = mul.m00 * point.x + mul.m01 * point.y + mul.m02 * point.z + mul.m03;
            result.y = mul.m10 * point.x + mul.m11 * point.y + mul.m12 * point.z + mul.m13;
            result.z = mul.m20 * point.x + mul.m21 * point.y + mul.m22 * point.z + mul.m23;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Minus3(this ref Vector3 a, in Vector3 b)
        {
            a.x -= b.x;
            a.y -= b.y;
            a.z -= b.z;
        }

        public static Rect Union(ref Rect rect1, ref Rect rect2)
        {
            if (rect2.width == 0 || rect2.height == 0)
                return rect1;

            if (rect1.width == 0 || rect1.height == 0)
                return rect2;

            float x = Mathf.Min(rect1.x, rect2.x);
            float y = Mathf.Min(rect1.y, rect2.y);
            return new Rect(x, y, Mathf.Max(rect1.xMax, rect2.xMax) - x, Mathf.Max(rect1.yMax, rect2.yMax) - y);
        }

        public static void SkewMatrix(ref Matrix4x4 matrix, float skewX, float skewY)
        {
            skewX = -skewX * Mathf.Deg2Rad;
            skewY = -skewY * Mathf.Deg2Rad;
            float sinX = Mathf.Sin(skewX);
            float cosX = Mathf.Cos(skewX);
            float sinY = Mathf.Sin(skewY);
            float cosY = Mathf.Cos(skewY);

            float m00 = matrix.m00 * cosY - matrix.m10 * sinX;
            float m10 = matrix.m00 * sinY + matrix.m10 * cosX;
            float m01 = matrix.m01 * cosY - matrix.m11 * sinX;
            float m11 = matrix.m01 * sinY + matrix.m11 * cosX;
            float m02 = matrix.m02 * cosY - matrix.m12 * sinX;
            float m12 = matrix.m02 * sinY + matrix.m12 * cosX;

            matrix.m00 = m00;
            matrix.m10 = m10;
            matrix.m01 = m01;
            matrix.m11 = m11;
            matrix.m02 = m02;
            matrix.m12 = m12;
        }

        public static void RotateUV(Vector2[] uv, ref Rect baseUVRect)
        {
            int vertCount = uv.Length;
            float xMin = Mathf.Min(baseUVRect.xMin, baseUVRect.xMax);
            float yMin = baseUVRect.yMin;
            float yMax = baseUVRect.yMax;
            if (yMin > yMax)
            {
                yMin = yMax;
                yMax = baseUVRect.yMin;
            }

            for (int i = 0; i < vertCount; i++)
            {
                Vector2 m = uv[i];
                var t = m.y;
                m.y = yMin + m.x - xMin;
                m.x = xMin + yMax - t;
                uv[i] = m;
            }
        }

        internal static bool CustomStartWith(string raw, string startWith)
        {
            int aLen = raw.Length;
            int bLen = startWith.Length;

            int ap = 0; int bp = 0;

            while (ap < aLen && bp < bLen && raw[ap] == startWith[bp])
            {
                ap++;
                bp++;
            }

            return (bp == bLen);
        }

        internal static bool GetFriendlyUrl(GObject obj, out string packageName, out string packageItemName)
        {
            if (obj != null && !string.IsNullOrEmpty(obj.resourceURL))
            {
                var pkgItem = UIPackage.GetItemByURL(obj.resourceURL);
                packageItemName = pkgItem.name;
                packageName = pkgItem.owner?.name;
                return true;
            }
            else
            {
                packageName = null;
                packageItemName = null;
                return false;
            }
        }

        internal static Span<T> AsSpan<T>(List<T> list)
        {
            Span<T> span = default;
            if (list is not null)
            {
                // int size = list._size;
                // T[] items = list._items;
                var listData = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<List<T>, ListDataHelper<T>>(ref list);
                int size = listData._size;
                T[] items = listData._items;
                Debug.Assert(items is not null, "Implementation depends on List<T> always having an array.");

                if ((uint)size > (uint)items.Length)
                {
                    // List<T> was erroneously mutated concurrently with this call, leading to a count larger than its array.
                    throw new InvalidOperationException("Concurrent operations are not supported.");
                }

                //Debug.Assert(typeof(T[]) == items.GetType(), "Implementation depends on List<T> always using a T[] and not U[] where U : T.");
                span = new Span<T>(items, 0, size);
            }

            return span;
        }
    }

    internal record ListDataHelper<T>
    {
        public T[] _items;
        public int _size;
        public int _version;
    }
}
