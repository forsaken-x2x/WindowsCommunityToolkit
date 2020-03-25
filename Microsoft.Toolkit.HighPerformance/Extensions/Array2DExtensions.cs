// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

namespace Microsoft.Toolkit.HighPerformance.Extensions
{
    /// <summary>
    /// Helpers for working with 2D <see cref="Array"/> instances.
    /// </summary>
    public static class Array2DExtensions
    {
        /// <summary>
        /// Returns a reference to the first element within a given 2D <typeparamref name="T"/> array, with no bounds checks.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input 2D <typeparamref name="T"/> array instance.</typeparam>
        /// <param name="array">The input <typeparamref name="T"/> array instance.</param>
        /// <returns>A reference to the first element within <paramref name="array"/>, or the location it would have used, if <paramref name="array"/> is empty.</returns>
        /// <remarks>This method doesn't do any bounds checks, therefore it is responsibility of the caller to perform checks in case the returned value is dereferenced.</remarks>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T DangerousGetReference<T>(this T[,] array)
        {
            var arrayData = Unsafe.As<RawArray2DData>(array);
            ref T r0 = ref Unsafe.As<byte, T>(ref arrayData.Data);

            return ref r0;
        }

        /// <summary>
        /// Returns a reference to an element at a specified coordinate within a given 2D <typeparamref name="T"/> array, with no bounds checks.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input 2D <typeparamref name="T"/> array instance.</typeparam>
        /// <param name="array">The input 2D <typeparamref name="T"/> array instance.</param>
        /// <param name="i">The vertical index of the element to retrieve within <paramref name="array"/>.</param>
        /// <param name="j">The horizontal index of the element to retrieve within <paramref name="array"/>.</param>
        /// <returns>A reference to the element within <paramref name="array"/> at the coordinate specified by <paramref name="i"/> and <paramref name="j"/>.</returns>
        /// <remarks>
        /// This method doesn't do any bounds checks, therefore it is responsibility of the caller to ensure the <paramref name="i"/>
        /// and <paramref name="j"/> parameters are valid. Furthermore, this extension will ignore the lower bounds for the input
        /// array, and will just assume that the input index is 0-based. It is responsability of the caller to adjust the input
        /// indices to account for the actual lower bounds, if the input array has either axis not starting at 0.
        /// </remarks>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T DangerousGetReferenceAt<T>(this T[,] array, int i, int j)
        {
            var arrayData = Unsafe.As<RawArray2DData>(array);
            int offset = (i * arrayData.Width) + j;
            ref T r0 = ref Unsafe.As<byte, T>(ref arrayData.Data);
            ref T ri = ref Unsafe.Add(ref r0, offset);

            return ref ri;
        }

        // Description adapted from CoreCLR: see https://source.dot.net/#System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeHelpers.CoreCLR.cs,285.
        // CLR 2D arrays are laid out in memory as follows:
        // [ sync block || pMethodTable || Length (padded to IntPtr) || HxW || HxW bounds || array data .. ]
        //                 ^                                                                 ^
        //                 |                                                                 \-- ref Unsafe.As<RawArray2DData>(array).Data
        //                 \-- array
        // The length is always padded to IntPtr just like with SZ arrays.
        // The total data padding is therefore 20 bytes on x86 (4 + 4 + 4 + 4 + 4), or 24 bytes on x64.
        [StructLayout(LayoutKind.Sequential)]
        private sealed class RawArray2DData
        {
#pragma warning disable CS0649 // Unassigned fields
#pragma warning disable SA1401 // Fields should be private
            public IntPtr Length;
            public int Height;
            public int Width;
            public int HeightLowerBound;
            public int WidthLowerBound;
            public byte Data;
#pragma warning restore CS0649
#pragma warning restore SA1401
        }

#if NETSTANDARD2_1
        /// <summary>
        /// Cretes a new <see cref="Span{T}"/> over an input 2D <typeparamref name="T"/> array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input 2D <typeparamref name="T"/> array instance.</typeparam>
        /// <param name="array">The input 2D <typeparamref name="T"/> array instance.</param>
        /// <returns>A <see cref="Span{T}"/> instance with the values of <paramref name="array"/>.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this T[,] array)
        {
            var arrayData = Unsafe.As<RawArray2DData>(array);

            /* On x64, the length is padded to x64, but it is represented in memory
             * as two sequential uint fields (one of which is padding).
             * So we can just reinterpret a reference to the IntPtr as one of type
             * uint, to access the first 4 bytes of that field, regardless of whether
             * we're running in a 32 or 64 bit process. This will work when on little
             * endian systems as well, as the memory layout for fields is the same,
             * the only difference is the order of bytes within each field of a given type.
             * We use checked here to follow suit with the CoreCLR source, where an
             * invalid value here should fail to perform the cast and throw an exception. */
            int length = checked((int)Unsafe.As<IntPtr, uint>(ref arrayData.Length));
            ref T r0 = ref Unsafe.As<byte, T>(ref arrayData.Data);

            return MemoryMarshal.CreateSpan(ref r0, length);
        }
#endif
    }
}
