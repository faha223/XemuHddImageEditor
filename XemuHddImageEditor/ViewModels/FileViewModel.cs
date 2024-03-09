using FatX.Net;
using File = FatX.Net.File;
namespace XemuHddImageEditor.ViewModels;

public class FileViewModel(File file)
{
    private File _file = file;

    public string Name => _file.Name;

    public void Extract()
    {
        // TODO: Implement this
    }
}