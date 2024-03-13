using InteropHelpers;
using QCow2.Net.Enums;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class QCow2Image
    {
        public FileHeader fileHeader;
        public List<HeaderExtensionRequiredData> headerExtensions = [];

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
            fileHeader = _source.Read<FileHeader>(true);
            PrintFileHeader(fileHeader);

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
                        var featureName = _source.Read<FeatureNameTableEntry>(false);
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

            // TODO: Read the RefcountTable and L1Table
        }

        private static void PrintFileHeader(FileHeader fileHeader)
        {
            Console.WriteLine($"Magic: \"{(char)(fileHeader.Magic >> 24)}{(char)((fileHeader.Magic & 0xff0000) >> 16)}{(char)((fileHeader.Magic & 0xff00) >> 8)}\\x{(fileHeader.Magic & 0xff):x}\"");
            Console.WriteLine($"Version: {fileHeader.Version}");
            Console.WriteLine($"BackingFileOffset: {fileHeader.BackingFileOffset}");
            Console.WriteLine($"BackingFileSize: {fileHeader.BackingFileSize}");
            Console.WriteLine($"ClusterBits: {fileHeader.ClusterBits}");
            Console.WriteLine($"ClusterSize: {(long)Math.Pow(2, fileHeader.ClusterBits)}");
            Console.WriteLine($"Size: {fileHeader.Size}");
            Console.WriteLine($"CryptMethod: {fileHeader.CryptMethod}");
            Console.WriteLine($"L1Size: {fileHeader.L1Size}");
            Console.WriteLine($"L1TableOffset: {fileHeader.L1TableOffset}");
            Console.WriteLine($"RefcountTableOffset: {fileHeader.RefcountTableOffset}");
            Console.WriteLine($"RefcountTableClusters: {fileHeader.RefcountTableClusters}");
            Console.WriteLine($"NumberOfSnapshots: {fileHeader.NumberOfSnapshots}");
            Console.WriteLine($"SnapshotsOffsets: {fileHeader.SnapshotsOffsets}");

            Console.WriteLine("--- The next values are expected to be 0 unless Version = 3+ ---");

            Console.WriteLine($"IncompatibleFeatures: {fileHeader.IncompatibleFeatures:B64}");
            Console.WriteLine($"CompatibleFeatures: {fileHeader.CompatibleFeatures:B64}");
            Console.WriteLine($"AutoclearFeatures: {fileHeader.AutoclearFeatures:B64}");
            Console.WriteLine($"RefcountOrder: {fileHeader.RefcountOrder}");
            Console.WriteLine($"RefcountBits: {1UL << (int)fileHeader.RefcountOrder}");
            Console.WriteLine($"HeaderLength: {fileHeader.HeaderLength}");
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