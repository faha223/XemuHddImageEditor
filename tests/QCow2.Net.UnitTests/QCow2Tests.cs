using System.Security.Cryptography;
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
        [Fact]
        public void TestConvertQCow2ToRaw()
        {
            var buffer = new byte[bufferSize];

            var fileStream = new FileStream(qcow2FilePath, FileMode.Open, FileAccess.Read);
            var rawFileStream = new FileStream(rawFilePath, FileMode.Open, FileAccess.Read);
            var qcow2Stream = new Qcow2Stream(fileStream);
            
            while(qcow2Stream.Position < qcow2Stream.Length)
            {
                var bytesRead = qcow2Stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }
            }

            var sha256 = SHA256.Create();
            var expectedHash = sha256.ComputeHash(rawFileStream);
            var actualHash = sha256.ComputeHash(fileStream);

            Assert.True(expectedHash.SequenceEqual(actualHash), "The hashes of the raw file and the qcow2 file do not match, indicating that the conversion was not successful.");
        }
    }
}