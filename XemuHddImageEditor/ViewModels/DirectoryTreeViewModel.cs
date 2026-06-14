using System.Collections.ObjectModel;
using FatX.Net;
using QCow2.Net;

namespace XemuHddImageEditor.ViewModels
{
    public class DirectoryTreeViewModel : ViewModelBase
    {
        private DiskImage? _img;
        
        private string _selectedDirectoryPath = string.Empty;
        public string SelectedDirectoryPath
        {
            get => _selectedDirectoryPath;
            set
            {
                _selectedDirectoryPath = value;
                _selectedDirectory = GetDirectoryViewModel(_selectedDirectoryPath);
                OnPropertyChanged(nameof(SelectedDirectoryPath));
                OnPropertyChanged(nameof(SelectedDirectory));
                SelectedDirectoryChanged?.Invoke(this, _selectedDirectoryPath);
            }
        }

        private DirectoryViewModel? _selectedDirectory;
        public DirectoryViewModel? SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                _selectedDirectory = value;
                _selectedDirectoryPath = _selectedDirectory?.FullName ?? string.Empty;
                OnPropertyChanged(nameof(SelectedDirectory));
                OnPropertyChanged(nameof(SelectedDirectoryPath));
                SelectedDirectoryChanged?.Invoke(this, _selectedDirectoryPath);
            }
        }

        public event EventHandler<string>? SelectedDirectoryChanged;

        public ObservableCollection<DirectoryViewModel> Subdirectories { get; } = [];

        public void LoadImage(string imgPath)
        {
            SelectedDirectory = null;
            Subdirectories.Clear();
            Stream stream = System.IO.File.Open(imgPath, FileMode.Open, FileAccess.ReadWrite);
            if(Path.GetExtension(imgPath).Equals(".qcow2", StringComparison.OrdinalIgnoreCase))
                stream = new QCow2Stream(stream);
            _img = new DiskImage(stream);
            
            foreach(var dir in _img.Partitions.Select(GetDirectoryViewModel))
                Subdirectories.Add(dir);
            
            if(Subdirectories.Count > 0)
                SelectedDirectoryPath = Subdirectories[0].FullName;
        }

        private static DirectoryViewModel GetDirectoryViewModel(Partition partition)
        {
            return new DirectoryViewModel(partition.GetRootDirectory().Result, null);
        }

        private DirectoryViewModel? GetDirectoryViewModel(string name) => GetDirectoryViewModel(Subdirectories, name);
        
        private static DirectoryViewModel? GetDirectoryViewModel(IEnumerable<DirectoryViewModel> directories, string name)
        {
            if(!string.IsNullOrWhiteSpace(name))
            {
                foreach (var dir in directories)
                {
                    if (dir.FullName == name)
                    {
                        return dir;
                    }
                    else if (name.StartsWith(dir.FullName))
                    {
                        return GetDirectoryViewModel(dir.Subdirectories, name);
                    }
                }
            }

            return null;
        }
    }
}
