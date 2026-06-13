using System.Runtime.InteropServices;
using InteropHelpers;
using QCow2.Net.Enums;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class QCow2Image
    {
        public ImageHeaderV3 ImageHeader {get; private set;}
        public List<HeaderExtensionRequiredData> headerExtensions { get; private set; } = [];

        public RefcountTable refcountTable = new();
        public L1Table l1Table = new();

        private Stream _source;

        public QCow2Image(string filename)
        {
            using FileStream source = new(filename, FileMode.Open);
            _source = source;
            Load();
        }

        public QCow2Image(Stream source)
        {
            _source = source;
            Load();
        }

        private void Load()
        {
            ImageHeader = _source.Read<ImageHeaderV3>(true);
            PrintFileHeader(ImageHeader);
            if(ImageHeader.IncompatibleFeatures.HasFlag(IncompatibleFeatures.Compression))
            {
                Console.WriteLine("File has Compression feature enabled.");
            }
            if(ImageHeader.HeaderLength > 104)
            {
                Console.WriteLine("File has optional bit field and padding");
                byte optionalBits = (byte)_source.ReadByte();
                Console.WriteLine($"Optional Bits: {optionalBits:B8}");

                // Consume the padding bytes until we reach the end of the header
                while(_source.Position < ImageHeader.HeaderLength)
                    _ = _source.ReadByte();
            }

            Console.WriteLine("--- Start of Header Extensions ---");
            var headerExtension = _source.Read<HeaderExtensionRequiredData>(true);
            while(headerExtension.Type != HeaderExtensionType.EndOfHeaderExtensionArea && headerExtension.Length > 0)
            {
                Console.WriteLine($"Header Extension Type: {headerExtension.Type}");
                
                if(headerExtension.Type == HeaderExtensionType.FeatureNameTable)
                {
                    // Read each entry
                    var numEntries = headerExtension.Length / 48;
                    Console.WriteLine($"Number of Features: {numEntries}");
                    for(uint i = 0; i < numEntries; i++)
                    {
                        Console.WriteLine("Feature:");
                        var featureName = _source.Read<FeatureNameTableEntry>(true);
                        PrintFeatureName(featureName);
                    }
                }
                else if(headerExtension.Type == HeaderExtensionType.BitmapsExtension)
                {
                    Console.WriteLine("---UNTESTED----");
                    var bmpExt = _source.Read<BitmapsExtension>(true);
                    PrintBitmapExtension(bmpExt);
                }
                else
                {
                    Console.WriteLine("Unexpected Extension Type. Ignoring.");
                    Console.WriteLine($"Header Extension Length: {headerExtension.Length}");
                    _source.Seek(headerExtension.Length, SeekOrigin.Current);
                }

                // Read Next Header
                headerExtension = _source.Read<HeaderExtensionRequiredData>(true);
            }

            Console.WriteLine("--- End of Header Extensions ---");

            // Read the RefcountTable and L1Table
        }

        public long GetClusterOffset(long offset)
        {
            long cluster_size = (long)Math.Pow(2, ImageHeader.ClusterBits);
            long l2_entries = cluster_size / sizeof(ulong);

            long l2_index = (offset / cluster_size) % l2_entries;
            long l1_index = (offset / cluster_size) / l2_entries;

            L2Table l2_table = new L2Table(_source, l1Table.GetEntry(l1_index).ImageOffset, (ulong)cluster_size);
            long cluster_offset = l2_table.GetEntry(l2_index / Marshal.SizeOf<L2TableEntry>()).HostClusterOffset; // Clear the top 2 bits which are used for flags);

            return cluster_offset + (offset % cluster_size);
        }

        private static void PrintFileHeader(ImageHeaderV3 imageHeader)
        {
            Console.WriteLine($"Magic: \"{(char)(imageHeader.Magic >> 24)}{(char)((imageHeader.Magic & 0xff0000) >> 16)}{(char)((imageHeader.Magic & 0xff00) >> 8)}\\x{(imageHeader.Magic & 0xff):x}\"");
            Console.WriteLine($"Version: {imageHeader.Version}");
            Console.WriteLine($"BackingFileOffset: {imageHeader.BackingFileOffset}");
            Console.WriteLine($"BackingFileSize: {imageHeader.BackingFileSize}");
            Console.WriteLine($"ClusterBits: {imageHeader.ClusterBits}");
            Console.WriteLine($"ClusterSize: {(long)Math.Pow(2, imageHeader.ClusterBits)}");
            Console.WriteLine($"Size: {imageHeader.Size}");
            Console.WriteLine($"CryptMethod: {imageHeader.CryptMethod}");
            Console.WriteLine($"L1Size: {imageHeader.L1Size}");
            Console.WriteLine($"L1TableOffset: {imageHeader.L1TableOffset}");
            Console.WriteLine($"RefcountTableOffset: {imageHeader.RefcountTableOffset}");
            Console.WriteLine($"RefcountTableClusters: {imageHeader.RefcountTableClusters}");
            Console.WriteLine($"NumberOfSnapshots: {imageHeader.NumberOfSnapshots}");
            Console.WriteLine($"SnapshotsOffsets: {imageHeader.SnapshotsOffsets}");

            Console.WriteLine("--- The next values are expected to be 0 unless Version = 3+ ---");

            Console.WriteLine($"IncompatibleFeatures: {imageHeader.IncompatibleFeatures}");
            Console.WriteLine($"CompatibleFeatures: {imageHeader.CompatibleFeatures}");
            Console.WriteLine($"AutoclearFeatures: {imageHeader.AutoclearFeatures}");
            Console.WriteLine($"RefcountOrder: {imageHeader.RefcountOrder}");
            Console.WriteLine($"RefcountBits: {1UL << (int)imageHeader.RefcountOrder}");
            Console.WriteLine($"HeaderLength: {imageHeader.HeaderLength}");
        }

        private static void PrintFeatureName(FeatureNameTableEntry featureName)
        {
            Console.WriteLine($"\tType: {featureName.Type}");
            Console.WriteLine($"\tBit Number: {featureName.BitNumber}");
            Console.WriteLine($"\tName: {featureName.Name}");
        }

        private static void PrintBitmapExtension(BitmapsExtension bmpExt)
        {
            Console.WriteLine($"Number of Bitmaps: {bmpExt.NumberOfBitmaps}");
            Console.WriteLine($"Reserved (0): {bmpExt.Reserved}");
            Console.WriteLine($"Bitmap Directory Size: {bmpExt.BitmapDirectorySize}");
            Console.WriteLine($"Bitmap Directory Offset: {bmpExt.BitmapDirectoryOffset}");
        }
    }
}