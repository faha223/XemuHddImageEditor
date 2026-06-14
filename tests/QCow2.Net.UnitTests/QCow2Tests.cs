using System.Runtime.InteropServices;
using System.Security.Cryptography;
using QCow2.Net.Structures;
using Xunit;

namespace QCow2.Net.UnitTests
{
    public class QCow2Tests
    {
        private const string hddImageDirectory = "/Users/fred/Xemu/HDD";
        private const string qcow2ImageFileName = "xbox_hdd_backup.qcow2";
        private const string rawImageFileName = "xbox_hdd_backup.img";
        private static readonly string qcow2FilePath = Path.Combine(hddImageDirectory, qcow2ImageFileName);
        private static readonly string rawFilePath = Path.Combine(hddImageDirectory, rawImageFileName);

        private const int bufferSize = 4 * 1024 * 1024; // 4 MiB

        static QCow2Tests()
        {
            Console.WriteLine("This system is little-endian: " + BitConverter.IsLittleEndian);
            ulong bits9Through55 = 0;
            for(int i = 9; i <= 55; i++)
                bits9Through55 |= (1UL << i);

            Console.WriteLine("Bits 9 through 55 mask: " + Constants.Bits9Through55.ToString("X"));
        }

        [Fact]
        public void TestQCow2HeaderSize()
        {
            const int headerSize = 104;
            int actualHeaderSize = Marshal.SizeOf<ImageHeaderV3>();
            Assert.Equal(headerSize, actualHeaderSize);
        }

        [Fact]
        public void TestConvertQCow2ToRaw()
        {
            var buffer = new byte[bufferSize];

            var fileStream = new FileStream(qcow2FilePath, FileMode.Open, FileAccess.Read);
            var rawFileStream = new FileStream(rawFilePath, FileMode.Open, FileAccess.Read);
            var qcow2Stream = new QCow2Stream(fileStream);

            var sha256 = SHA256.Create();

            // Compare their lengths first, if they are not the same, there is no need to compare the contents
            Assert.Equal(rawFileStream.Length, qcow2Stream.Length);

            byte[] expectedPage = new byte[4096];
            byte[] actualPage = new byte[4096];
            while(rawFileStream.Position < rawFileStream.Length)
            {
                rawFileStream.ReadExactly(expectedPage, 0, expectedPage.Length);
                qcow2Stream.ReadExactly(actualPage, 0, actualPage.Length);

                Assert.True(expectedPage.SequenceEqual(actualPage), $"The contents of the raw file and the qcow2 file do not match at position {rawFileStream.Position - expectedPage.Length}. This indicates that the conversion was not successful.");
            }

            // Compute the hash of both streams in chunks to avoid loading the entire file into memory
            // These are 8.0 GiB files, so we don't want to load them into memory
            //var expectedHash = sha256.ComputeHash(rawFileStream);
            //var actualHash = sha256.ComputeHash(qcow2Stream);

            //Assert.True(expectedHash.SequenceEqual(actualHash), "The hashes of the raw file and the qcow2 file do not match, indicating that the conversion was not successful.");
        }
    }
}