using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Atrufulgium.EternalDreamCatcher.Base {

    /// <summary>
    /// A <see cref="NativeArray{T}"/> with the safety features ripped out
    /// to make it blittable.
    /// </summary>
    /// <remarks>
    /// I blindly ripped out stuff without thinking, and writing tests for
    /// memory safety is obnoxious, so I guess I'll see the segfaults happen
    /// when they happen.
    /// </remarks>
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(UnsafeArrayDebugView<>))]
    public struct UnsafeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<UnsafeArray<T>> where T : unmanaged {
        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable {
            private UnsafeArray<T> m_Array;

            private int m_Index;

            public T Current => m_Array[m_Index];

            object IEnumerator.Current => Current;

            public Enumerator(ref UnsafeArray<T> array) {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose() {
            }

            public bool MoveNext() {
                m_Index++;
                return m_Index < m_Array.Length;
            }

            public void Reset() {
                m_Index = -1;
            }
        }

        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* m_Buffer;

        public unsafe T* Ptr => (T*)m_Buffer;

        internal int m_Length;

        internal int m_MinIndex;

        internal int m_MaxIndex;

        internal Allocator m_AllocatorLabel;

        public int Length => m_Length;

        public unsafe T this[int index] {
            get {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }
            [WriteAccessRequired]
            set {
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        public unsafe bool IsCreated => m_Buffer != null;

        public unsafe UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory) {
                UnsafeUtility.MemClear(m_Buffer, (long)Length * (long)UnsafeUtility.SizeOf<T>());
            }
        }

        public UnsafeArray(T[] array, Allocator allocator) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public UnsafeArray(UnsafeArray<T> array, Allocator allocator) {
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator, long totalSize) {
            if (allocator <= Allocator.None) {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
            }

            if (length < 0) {
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");
            }

            IsUnmanagedAndThrow();
        }

        private unsafe static void Allocate(int length, Allocator allocator, out UnsafeArray<T> array) {
            long num = (long)UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator, num);
            array = default(UnsafeArray<T>);
            array.m_Buffer = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;
            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
        }

        [BurstDiscard]
        internal static void IsUnmanagedAndThrow() {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>()) {
                throw new InvalidOperationException($"{typeof(T)} used in UnsafeArray<{typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementReadAccess(int index) {
            if (index < m_MinIndex || index > m_MaxIndex) {
                FailOutOfRangeError(index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementWriteAccess(int index) {
            if (index < m_MinIndex || index > m_MaxIndex) {
                FailOutOfRangeError(index);
            }
        }

        [WriteAccessRequired]
        public unsafe void Dispose() {
            if (m_Buffer == null) {
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            }

            if (m_AllocatorLabel == Allocator.Invalid) {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel > Allocator.None) {
                UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array) {
            Copy(array, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array) {
            Copy(array, this);
        }

        public void CopyTo(T[] array) {
            Copy(this, array);
        }

        public void CopyTo(UnsafeArray<T> array) {
            Copy(this, array);
        }

        public T[] ToArray() {
            T[] array = new T[Length];
            Copy(this, array, Length);
            return array;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index) {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1)) {
                throw new IndexOutOfRangeException($"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\n" + "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }

            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(ref this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public unsafe bool Equals(UnsafeArray<T> other) {
            return m_Buffer == other.m_Buffer && m_Length == other.m_Length;
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }

            return obj is UnsafeArray<T> && Equals((UnsafeArray<T>)obj);
        }

        public unsafe override int GetHashCode() {
            return ((int)m_Buffer * 397) ^ m_Length;
        }

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) {
            return !left.Equals(right);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength) {
            if (srcLength != dstLength) {
                throw new ArgumentException("source and destination length must be the same");
            }
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst) {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst) {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst) {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length) {
            Copy(src, 0, dst, 0, length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst, int length) {
            Copy(src, 0, dst, 0, length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst, int length) {
            Copy(src, 0, dst, 0, length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length) {
            if (length < 0) {
                throw new ArgumentOutOfRangeException("length", "length must be equal or greater than zero.");
            }

            if (srcIndex < 0 || srcIndex > srcLength || (srcIndex == srcLength && srcLength > 0)) {
                throw new ArgumentOutOfRangeException("srcIndex", "srcIndex is outside the range of valid indexes for the source UnsafeArray.");
            }

            if (dstIndex < 0 || dstIndex > dstLength || (dstIndex == dstLength && dstLength > 0)) {
                throw new ArgumentOutOfRangeException("dstIndex", "dstIndex is outside the range of valid indexes for the destination UnsafeArray.");
            }

            if (srcIndex + length > srcLength) {
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.", "length");
            }

            if (srcIndex + length < 0) {
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            }

            if (dstIndex + length > dstLength) {
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.", "length");
            }

            if (dstIndex + length < 0) {
                throw new ArgumentException("dstIndex + length causes an integer overflow");
            }
        }

        public unsafe static void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length) {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(), (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(), length * UnsafeUtility.SizeOf<T>());
        }

        public unsafe static void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length) {
            if (src == null) {
                throw new ArgumentNullException("src");
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gCHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            IntPtr intPtr = gCHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(), (byte*)(void*)intPtr + srcIndex * UnsafeUtility.SizeOf<T>(), length * UnsafeUtility.SizeOf<T>());
            gCHandle.Free();
        }

        public unsafe static void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length) {
            if (dst == null) {
                throw new ArgumentNullException("dst");
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gCHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            IntPtr intPtr = gCHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((byte*)(void*)intPtr + dstIndex * UnsafeUtility.SizeOf<T>(), (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(), length * UnsafeUtility.SizeOf<T>());
            gCHandle.Free();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class UnsafeArrayDebugView<T> where T : unmanaged {
        private UnsafeArray<T> m_Array;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => m_Array.ToArray();

        public UnsafeArrayDebugView(UnsafeArray<T> array) {
            m_Array = array;
        }
    }
}