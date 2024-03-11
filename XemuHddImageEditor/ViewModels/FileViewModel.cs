using XemuHddImageEditor.Views;
using File = FatX.Net.File;
using XemuHddImageEditor.Helpers;

namespace XemuHddImageEditor.ViewModels;

public class FileViewModel(File file, DirectoryViewModel parentDirectory) : ViewModelBase
{
    public DirectoryViewModel ParentDirectory { get; init; } = parentDirectory;
    
    private File _file = file;

    public string FullName => _file.FullName;

    public string Name
    {
        get => _file.Name;
        set
        {
            _file.Name = value;
            OnPropertyChanged(nameof(Name)); 
            OnPropertyChanged(nameof(FullName));
        }
    }

    public async Task Extract()
    {
        var result = await DialogHelpers.SaveFileDialog(Name);
        if (result != null)
        {
            await _file.Extract(result);
        }
    }

    public async Task Rename()
    {
        var vm = new RenameFileModalViewModel(this);
        var dialog = new RenameFileModal()
        {
            ViewModel = vm
        };

        await DialogHelpers.ShowDialog<RenameFileModalViewModel?>(dialog);
        
        if (vm.DialogResult)
            Name = vm.NewFilename;
    }
}