using FatX.Net;
using System.Linq;

namespace XemuHddImageEditor.ViewModels;

public class MainWindowViewModel
{
    public List<DirectoryViewModel> Subdirectories { get; init; }

    public MainWindowViewModel()
    {
        var img = new DiskImage("/home/fred/Xemu/HDD/xemu_hdd_tool_test.raw");
        var list = new List<DirectoryViewModel>(img.Partitions.Count);
        foreach(var partition in img.Partitions)
        {
            var vm = GetDirectoryViewModel(partition);
            list.Add(vm);
        }
        Subdirectories = list;
    }

    private static DirectoryViewModel GetDirectoryViewModel(Partition partition)
    {
        return new DirectoryViewModel(partition.GetRootDirectory().Result);
    }
}