using FatX.Net;

namespace XemuHddImageEditor.ViewModels
{
    public class DirectoryTreeViewModel
    {
        private string _selectedDirectory = "C";
        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                _selectedDirectory = value;
                SelectedDirectoryChanged?.Invoke(this, _selectedDirectory);
            }
        }

        public event EventHandler<string>? SelectedDirectoryChanged;

        public List<DirectoryViewModel> Subdirectories { get; } = [];

        public void LoadImage(string imgPath)
        {
            Subdirectories.Clear();
            var img = new DiskImage(imgPath);
            var list = new List<DirectoryViewModel>(img.Partitions.Count);
            foreach (var partition in img.Partitions)
            {
                var vm = GetDirectoryViewModel(partition);
                list.Add(vm);
            }
            Subdirectories.AddRange(list);
        }

        private static DirectoryViewModel GetDirectoryViewModel(Partition partition)
        {
            return new DirectoryViewModel(partition.GetRootDirectory().Result);
        }
    }
}
