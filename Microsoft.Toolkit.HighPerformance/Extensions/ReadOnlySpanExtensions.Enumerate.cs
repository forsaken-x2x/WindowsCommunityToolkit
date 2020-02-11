﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Toolkit.HighPerformance.Extensions
{
    /// <summary>
    /// Helpers for working with the <see cref="ReadOnlySpan{T}"/> type.
    /// </summary>
    public static partial class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Enumerates the items in the input <see cref="ReadOnlySpan{T}"/> instance, as pairs of value/index values.
        /// This extension should be used directly within a <see langword="foreach"/> loop:
        /// <code>
        /// ReadOnlySpan&lt;string&gt; words = new[] { "Hello", ", ", "world", "!" };
        ///
        /// foreach (var item in words.Enumerate())
        /// {
        ///     // Access the index and value of each item here...
        ///     int index = item.Index;
        ///     string value = item.Value;
        /// }
        /// </code>
        /// The compiler will take care of properly setting up the <see langword="foreach"/> loop with the type returned from this method.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <param name="span">The source <see cref="ReadOnlySpan{T}"/> to enumerate.</param>
        /// <returns>A wrapper type that will handle the value/index enumeration for <paramref name="span"/>.</returns>
        /// <remarks>The returned <see cref="__Enumerator{T}"/> value shouldn't be used directly: use this extension in a <see langword="foreach"/> loop.</remarks>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static __Enumerator<T> Enumerate<T>(this ReadOnlySpan<T> span)
        {
            return new __Enumerator<T>(span);
        }

        /// <summary>
        /// A <see langword="ref"/> <see langword="struct"/> that enumerates the items in a given <see cref="ReadOnlySpan{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300", Justification = "The type is not meant to be used directly by users")]
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1206", Justification = "The type is a ref struct")]
        public readonly ref struct __Enumerator<T>
        {
            /// <summary>
            /// The source <see cref="ReadOnlySpan{T}"/> instance
            /// </summary>
            private readonly ReadOnlySpan<T> span;

            /// <summary>
            /// Initializes a new instance of the <see cref="__Enumerator{T}"/> struct.
            /// </summary>
            /// <param name="span">The source <see cref="ReadOnlySpan{T}"/> to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public __Enumerator(ReadOnlySpan<T> span)
            {
                this.span = span;
            }

            /// <summary>
            /// Implements the duck-typed <see cref="IEnumerable{T}.GetEnumerator"/> method.
            /// </summary>
            /// <returns>An <see cref="Enumerator"/> instance targeting the current <see cref="ReadOnlySpan{T}"/> value.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() => new Enumerator(this.span);

            /// <summary>
            /// An enumerator for a source <see cref="ReadOnlySpan{T}"/> instance.
            /// </summary>
            public ref struct Enumerator
            {
                /// <summary>
                /// The source <see cref="ReadOnlySpan{T}"/> instance.
                /// </summary>
                private readonly ReadOnlySpan<T> span;

                /// <summary>
                /// The current index within <see cref="span"/>.
                /// </summary>
                private int index;

                /// <summary>
                /// Initializes a new instance of the <see cref="Enumerator"/> struct.
                /// </summary>
                /// <param name="span">The source <see cref="ReadOnlySpan{T}"/> instance.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Enumerator(ReadOnlySpan<T> span)
                {
                    this.span = span;
                    this.index = -1;
                }

                /// <summary>
                /// Implements the duck-typed <see cref="System.Collections.IEnumerator.MoveNext"/> method.
                /// </summary>
                /// <returns><see langword="true"/> whether a new element is available, <see langword="false"/> otherwise</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    int newIndex = this.index + 1;

                    if (newIndex < this.span.Length)
                    {
                        this.index = newIndex;

                        return true;
                    }

                    return false;
                }

                /// <summary>
                /// Gets the duck-typed <see cref="IEnumerator{T}.Current"/> property.
                /// </summary>
                [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1008", Justification = "ValueTuple<T1,T2> return type")]
                public (int Index, T Value) Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        int currentIndex = this.index;
                        T value = Unsafe.Add(ref MemoryMarshal.GetReference(this.span), currentIndex);

                        return (currentIndex, value);
                    }
                }
            }
        }
    }
}
