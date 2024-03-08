using FatX.Net;
using System.Runtime.InteropServices;

namespace XemuHddImageEditor
{
    public static class Program
    {
        const string rawSourceImageWindows = "C:\\Users\\speci\\AppData\\Roaming\\xemu\\xemu\\HDD\\xemu_hdd_tool_test.raw";
        const string rawSourceImage = "/home/fred/Xemu/HDD/xemu_hdd_tool_test.raw";

        const string extractDir ="/home/fred/temp";
        const string extractDirWindows = "F:\\temp";

        public static void Main(string[] args)
        {
            OperationMode? mode = null;
            string? imagePath = null;
            string? targetDirectory = null;
            string? searchQuery = null;

            if(args.Length > 0)
            {
                for(int i = 0; i < args.Length; i++)
                {
                    switch(args[i].ToLowerInvariant())
                    {
                        case "-i":
                        case "--image":
                            imagePath = args[++i];
                            break;
                        case "-o":
                        case "--out":
                            targetDirectory = args[++i];
                            break;
                        case "-q":
                        case "--query":
                            searchQuery = args[++i];
                            break;
                        case "-m":
                        case "--mode":
                            var modeStr = args[++i];
                            if(Enum.TryParse<OperationMode>(modeStr, out var newMode))
                            {
                                mode = newMode;
                            }
                            else
                            {
                                Console.WriteLine("Unrecognized Mode: " + modeStr);
                                return;
                            }
                            break;
                        default:
                            Console.WriteLine("Unexpected Argument: " + args[i]);
                            return;
                    }
                }
            }

            if(mode == null)
            {
                mode = OperationMode.List;
                Console.WriteLine("Mode not Specified. Defaulting to " + mode);
            }

            if(imagePath == null)
            {
                imagePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    rawSourceImageWindows :
                    rawSourceImage;
                Console.WriteLine("Image Path not specified. Defaulting to " + imagePath);
            }

            var image = new DiskImage(imagePath);
            switch(mode)
            {
                case OperationMode.Extract:
                    if (targetDirectory == null)
                    {
                        targetDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                            extractDirWindows : 
                            extractDir;
                        Console.WriteLine("Output Directory not specified, defaulting to " + targetDirectory);
                    }
                    ExtractImage(image, targetDirectory).Wait();
                    break;
                case OperationMode.List:
                    ListContents(image).Wait();
                    break;
                case OperationMode.Search:
                    if(searchQuery == null)
                    {
                        searchQuery = "**" + Path.DirectorySeparatorChar + "*";
                        Console.WriteLine("SearchQuery not specified, defaulting to " + searchQuery);
                    }
                    Search(image, searchQuery).Wait();
                    break;
            }
            
            Console.WriteLine("done.");
            Console.ReadKey();
        }

        private static async Task ExtractImage(DiskImage image, string targetDirectory)
        {
            await image.Extract(targetDirectory);
        }

        // private static async Task ExtractFromImage(DiskImage image, string sourcePath, string targetDirectory)
        // {
        //     await image.Extract(sourcePath, targetDirectory);
        // }

        private static async Task ListContents(DiskImage image)
        {
            Console.WriteLine("Printing the Contents of the Image");
            foreach(var partition in image.Partitions)
            {
                var root = await partition.GetRootDirectory();
                await root.PrintTree();
            }
        }

        private static async Task Search(DiskImage image, string query)
        {
            Console.WriteLine("Searching for " + query);
            foreach(var result in await image.Search(query))
            {
                Console.WriteLine("  " + result);
            }
        }
    }
}
