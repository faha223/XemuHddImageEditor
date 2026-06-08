using FatX.Net;
using FatX.Net.Enums;
using FatX.Net.Structures;  
using InteropHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace FatX.Net.UnitTests;

static class TestData
{
    private const long ImageSize = 8589934592L; // 8 GB
    private static readonly byte[] EmptyCluster = new byte[Constants.SectorSize512 * 8];
    public static void GenerateTestImage(string path)
    {
        // Generate an empty 8 GB file at the path
        if(System.IO.File.Exists(path))
            System.IO.File.Delete(path);
        using var fileStream = System.IO.File.Create(path);
        while(fileStream.Position < ImageSize)
            fileStream.Write(EmptyCluster, 0, EmptyCluster.Length);

        // Go to the start of the stream
        fileStream.Seek(0, SeekOrigin.Begin);

        try
        {
            WriteEmptyPartition(fileStream, Constants.CPartitionOffset, Constants.CPartitionSize);
            WriteEmptyPartition(fileStream, Constants.EPartitionOffset, Constants.EPartitionSize);
            WriteEmptyPartition(fileStream, Constants.XPartitionOffset, Constants.XPartitionSize);
            WriteEmptyPartition(fileStream, Constants.YPartitionOffset, Constants.YPartitionSize);
            WriteEmptyPartition(fileStream, Constants.ZPartitionOffset, Constants.ZPartitionSize);
        }
        finally
        {
            fileStream.Flush();
            fileStream.Close();
        }
    }

    private static void WriteEmptyPartition(FileStream fileStream, long offset, long size)
    {
        // Use a substream to restrict read/write access to the partition area
        using var stream = new Substream(fileStream, offset, size);

        // Write the superblock at the beginning of the partition
        Superblock superblock = new Superblock
        {
            Signature = Constants.FATX_Signature,
            VolumeId = 0x12345678, // Arbitrary volume ID
            SectorsPerCluster = 8, // 4 KB clusters
            RootCluster = 1,
            Unknown = 0
        };
        stream.Write(superblock);

        // Write the FAT, reserving the first cluster
        stream.Seek(Constants.FATX_FAT_Offset, SeekOrigin.Begin);
        
        var bytesPerCluster = superblock.SectorsPerCluster * Constants.SectorSize512;
        var fatSize = size / bytesPerCluster;
        fatSize *= 4;
        /* Round FAT size up to nearest 4k boundary. */
        if (fatSize % 4096 != 0)
            fatSize += 4096 - (fatSize % 4096);
        var FAT = new byte[fatSize];

        // Write the reserved entry in the FAT table at offset 4, in Big Endian format
        FAT[0] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0xFF000000) >> 24);
        FAT[1] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x00FF0000) >> 16);
        FAT[2] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x0000FF00) >> 8);
        FAT[3] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x000000FF) >> 0);
        FAT[4] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0xFF000000) >> 24);
        FAT[5] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x00FF0000) >> 16);
        FAT[6] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x0000FF00) >> 8);
        FAT[7] = (byte)((Constants.FATX_CLUSTER_RESERVED_32 & 0x000000FF) >> 0);
        stream.Write(FAT, 0, FAT.Length);

        // Seek to the first Cluster
        stream.Seek(Constants.FATX_FAT_Offset + FAT.Length + (Constants.FATX_FAT_ReservedEntriesCount * bytesPerCluster), SeekOrigin.Begin);
        stream.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker); // Write the end of directory marker to signify that the directory is empty
    }
}