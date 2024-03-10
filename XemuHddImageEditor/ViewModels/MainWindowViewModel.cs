using FatX.Net;
using System.Runtime.InteropServices;

namespace XemuHddImageEditor.ViewModels;

public class MainWindowViewModel
{
    public DirectoryTreeViewModel DirectoryTreeVM { get; } = new();

    public DirectoryViewModel? SelectedDirectory { get; set; } = null;

    public MainWindowViewModel()
    {
        var imgPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            "C:\\Users\\speci\\AppData\\Roaming\\xemu\\xemu\\HDD\\xemu_hdd_tool_test.raw" :
            "/home/fred/Xemu/HDD/xemu_hdd_tool_test.raw";

        DirectoryTreeVM.SelectedDirectoryChanged += DirectoryTreeVM_SelectedDirectoryChanged;
        DirectoryTreeVM.LoadImage(imgPath);

        DirectoryTreeVM.SelectedDirectory = "C";
    }

    private DirectoryViewModel? GetDirectoryViewModel(List<DirectoryViewModel> directories, string name)
    {
        foreach (var dir in directories)
        {
            if (dir.Name == name)
            {
                return dir;
            }
            else if (name.StartsWith(dir.Name))
            {
                return GetDirectoryViewModel(dir.Subdirectories, name);
            }
        }

        return null;
    }

    private void DirectoryTreeVM_SelectedDirectoryChanged(object? sender, string newValue)
    {
        // IDK
        SelectedDirectory = GetDirectoryViewModel(DirectoryTreeVM.Subdirectories, newValue);
    }
}