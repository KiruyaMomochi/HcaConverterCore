using System;
using System.IO;
using System.Security.Cryptography;

namespace DereTore.Exchange.Archive.ACB {
    public static class AcbHelper {

        public static Stream ExtractToNewStream(Stream stream, long offset, int length) {
            var originalPosition = stream.Position;
            stream.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[length];
            var memory = new byte[length];
            long currentIndex = 0;
            var bytesLeft = length;
            do {
                var read = stream.Read(buffer, 0, bytesLeft);
                Array.Copy(buffer, 0, memory, currentIndex, read);
                currentIndex += read;
                bytesLeft -= read;
            } while (bytesLeft > 0);
            stream.Position = originalPosition;
            var memoryStream = new MemoryStream(memory, false) {
                Capacity = length
            };
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        internal static bool AreDataIdentical(byte[] array1, long offset1, byte[] array2, long offset2, long length) {
            if (length <= 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (offset1 < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset1));
            }
            if (offset2 < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset2));
            }
            long end1 = offset1 + length, end2 = offset2 + length;
            if (end1 > array1.LongLength) {
                throw new IndexOutOfRangeException("Length of array 1 is not enough.");
            }
            if (end2 > array2.LongLength) {
                throw new IndexOutOfRangeException("Length of array 2 is not enough.");
            }
            for (; offset1 < end1; ++offset1, ++offset2) {
                if (array1[offset1] != array2[offset2]) {
                    return false;
                }
            }
            return true;
        }

        internal static bool AreDataIdentical(byte[] array1, int offset1, byte[] array2, int offset2, int length) {
            return AreDataIdentical(array1, offset1, array2, offset2, (long)length);
        }

        internal static bool AreDataIdentical(byte[] array1, byte[] array2, long length) {
            return AreDataIdentical(array1, 0, array2, 0, length);
        }

        internal static bool AreDataIdentical(byte[] array1, byte[] array2, int length) {
            return AreDataIdentical(array1, 0, array2, 0, (long)length);
        }

        internal static bool AreDataIdentical(byte[] array1, byte[] array2) {
            return AreDataIdentical(array1, 0, array2, 0, array1.LongLength);
        }

        internal static int RoundUpToAlignment(int valueToRound, int byteAlignment) {
            var roundedValue = (valueToRound + byteAlignment - 1) / byteAlignment * byteAlignment;
            return roundedValue;
        }

        internal static uint RoundUpToAlignment(uint valueToRound, uint byteAlignment) {
            var roundedValue = (valueToRound + byteAlignment - 1) / byteAlignment * byteAlignment;
            return roundedValue;
        }

        internal static long RoundUpToAlignment(long valueToRound, long byteAlignment) {
            var roundedValue = (valueToRound + byteAlignment - 1) / byteAlignment * byteAlignment;
            return roundedValue;
        }

        internal static ulong RoundUpToAlignment(ulong valueToRound, ulong byteAlignment) {
            var roundedValue = (valueToRound + byteAlignment - 1) / byteAlignment * byteAlignment;
            return roundedValue;
        }

        internal static byte[] GetMd5Checksum(Stream stream) {
            return Md5.ComputeHash(stream);
        }

        internal static byte[] GetMd5Checksum(byte[] data) {
            return Md5.ComputeHash(data);
        }

        private static readonly MD5 Md5 = MD5.Create();

    }
}
