using Avalonia.Controls;
using Avalonia.Input;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views
{
    public partial class Directory : UserControl
    {
        public Directory()
        {
            InitializeComponent();
            AddHandler(DragDrop.DropEvent, OnDrop);
        }

        public void DoubleClick(object sender, TappedEventArgs args)
        {
            if(sender is Control control)
            {
                if(control.DataContext is DirectoryViewModel vm)
                {
                    _ = vm.Open();
                }
            }
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[Debug] Views.Directory.OnDrop");
            if (e.DataTransfer.Formats.Contains(DataFormat.File))
            {
                var files = e.DataTransfer.TryGetFiles();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        // Process each dropped file
                        System.Diagnostics.Debug.WriteLine($"Dropped: {file.Name}");
                    }
                }
            }
        }
    }
}
