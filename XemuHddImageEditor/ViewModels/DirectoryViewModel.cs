using Directory = FatX.Net.Directory;
using File = FatX.Net.File;
using FatX.Net;

namespace XemuHddImageEditor.ViewModels;

public class DirectoryViewModel(Directory directory)
{
    private Directory _directory = directory;

    public string Name => _directory.Name;

    private List<DirectoryViewModel>? _subdirectories;
    public List<DirectoryViewModel> Subdirectories
    {
        get
        {
            if(_subdirectories == null)
                _subdirectories = _directory.Subdirectories.Select(d => new DirectoryViewModel(d)).ToList();

            return _subdirectories;
        }
    }

    private List<FileViewModel>? _files;
    public List<FileViewModel> Files
    {
        get
        {
            if(_files == null)
                _files = _directory.Files.Select(f => new FileViewModel(f)).ToList();
            return _files;
        }
    }

    public List<object> Contents
    {
        get
        {
            var list = new List<object>();
            list.AddRange(Subdirectories);
            list.AddRange(Files);
            return list;
        }
    }

    public bool ShowFiles{ get; set; } = true;

    public void Extract()
    {
        // TODO: Implement this
    }
}