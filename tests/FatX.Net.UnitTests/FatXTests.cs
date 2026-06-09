using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FatX.Net.UnitTests;

public class FatXTests
{
    //[Fact]
    /// <summary> This function generates an empty Xemu HDD image, initializes each partition, and verifies 
    /// that each partition contains no files or subdirectories. </summary>
    public void TestGenerateEmptyXemuHddImage()
    {

        string testImagePath = nameof(TestGenerateEmptyXemuHddImage) + ".img";
        using var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite);
        Dictionary<char, Filesystem> partitions = new Dictionary<char, Filesystem>();
        List<(char, long, long)> partitionInfo = new List<(char, long, long)>
        {
            ('C', Constants.CPartitionOffset, Constants.CPartitionSize),
            ('E', Constants.EPartitionOffset, Constants.EPartitionSize),
            ('X', Constants.XPartitionOffset, Constants.XPartitionSize),
            ('Y', Constants.YPartitionOffset, Constants.YPartitionSize),
            ('Z', Constants.ZPartitionOffset, Constants.ZPartitionSize)
        };
        foreach(var tuple in partitionInfo)
        {
            var partition = new Filesystem(fileStream, tuple.Item1, tuple.Item2, tuple.Item3);
            partition.Init();
            partitions.Add(tuple.Item1, partition);
        }

        foreach(var entry in partitions)
        {
            Assert.True(entry.Value.Initialized);
            Assert.Equal(entry.Key, entry.Value.DriveLetter);
            var rootDirectory = entry.Value.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);
            Assert.Equal(entry.Key.ToString(), rootDirectory.Name);
            Assert.Equal(0, rootDirectory.Files.Count);
            Assert.Equal(0, rootDirectory.Subdirectories.Count);
        }

        System.IO.File.Delete(testImagePath);
    }

    // [Fact]
    /// <summary> This function creates a subdirectory in the root directory of the C partition and verifies 
    /// that the subdirectory is created correctly. </summary>
    public void TestCreateSubdirectory()
    {
        // Generate a test image
        string testImagePath = nameof(TestCreateSubdirectory) + ".img";
        TestData.GenerateTestImage(testImagePath);
        string testSubdirName = "TestSubdir";
        string testSubdirName2 = "TestSubdir2";
        string testSubdirName3 = "TestSubdir3";

        using (var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Open the file for writing the new subdirectory to the filesystem
            var partitionForWrite = new Filesystem(fileStream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize);
            
            partitionForWrite.Init();

            var rootDirectory = partitionForWrite.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);

            var subdirectory = rootDirectory.CreateSubdirectory(testSubdirName).Result;
            fileStream.Flush();
        }

        using (var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Open the file fresh for reading
            var partitionForRead = new Filesystem(fileStream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize);        
            partitionForRead.Init();

            // Get the Root directory
            var rootDirectory = partitionForRead.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);

            // Verify that the Root directory has exactly one subdirectory with the correct name
            Assert.Single(rootDirectory.Subdirectories);
            Assert.Equal(testSubdirName, rootDirectory.Subdirectories[0].Name);

            // Get the subdirectory and verify that it doesn't contain any files or subdirectories
            var testSubdir = rootDirectory.Subdirectories[0];
            Assert.Empty(testSubdir.Files);
            Assert.Empty(testSubdir.Subdirectories);

            // Create a subdirectory in the first subdirectory
            testSubdir.CreateSubdirectory(testSubdirName2).Wait();
            rootDirectory.PrintTree();

            // Create another subdirectory in the root directory
            rootDirectory.CreateSubdirectory(testSubdirName3).Wait();
            rootDirectory.PrintTree();
            fileStream.Flush();
        }

        using (var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Open the file fresh for reading
            var partitionForRead = new Filesystem(fileStream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize);        
            partitionForRead.Init();

            var rootDirectory = partitionForRead.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);

            rootDirectory.PrintTree();

            Assert.Equal(2, rootDirectory.Subdirectories.Count);

            var testSubdir = rootDirectory.Subdirectories[0];
            Assert.Equal(testSubdirName, testSubdir.Name);
            Assert.Empty(testSubdir.Files);
            Assert.Single(testSubdir.Subdirectories);

            var testSubdir2 = testSubdir.Subdirectories[0];
            Assert.Equal(testSubdirName2, testSubdir2.Name);
            Assert.Empty(testSubdir2.Files);
            Assert.Empty(testSubdir2.Subdirectories);

            var testSubdir3 = rootDirectory.Subdirectories[1];
            Assert.Equal(testSubdirName3, testSubdir3.Name);
            Assert.Empty(testSubdir3.Files);
            Assert.Empty(testSubdir3.Subdirectories);

            // The three subdirectories should be allocated to different clusters
            Assert.NotEqual(testSubdir.Cluster, testSubdir2.Cluster); 
            Assert.NotEqual(testSubdir2.Cluster, testSubdir3.Cluster); 
        }

        // Delete the test image
        System.IO.File.Delete(testImagePath);
    }

    [Fact]
    public void TestCreateFile()
    {
        // Generate a test image
        string testFileName = "/Users/fred/Xemu/BIOS/Complex 4627 Retail 1.03.bin";
        var fileInfo = new FileInfo(testFileName);
        string testImagePath = nameof(TestCreateFile) + ".img";
        TestData.GenerateTestImage(testImagePath);

        using (var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Open the file for writing the new file to the filesystem
            var partitionForWrite = new Filesystem(fileStream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize);
            
            partitionForWrite.Init();

            var rootDirectory = partitionForWrite.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);

            var contentStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
            rootDirectory.CreateFile(fileInfo, contentStream).Wait();
            fileStream.Flush();
        }

        using (var fileStream = new FileStream(testImagePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Open the file fresh for reading
            var partitionForRead = new Filesystem(fileStream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize);        
            partitionForRead.Init();

            var rootDirectory = partitionForRead.GetRootDirectory().Result;
            Assert.NotNull(rootDirectory);
            rootDirectory.PrintTree();
            Assert.Single(rootDirectory.Files);
            var file = rootDirectory.Files[0];
            Assert.Equal(fileInfo.Name, file.Name);
            file.Extract("temp.bin").Wait();
            Assert.True(System.IO.File.ReadAllBytes("temp.bin").SequenceEqual(System.IO.File.ReadAllBytes(testFileName)));
        }
    }
}