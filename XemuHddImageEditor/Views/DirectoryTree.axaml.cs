using Avalonia.Controls;
using Avalonia.Interactivity;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views
{
    public partial class DirectoryTree : UserControl
    {
        public DirectoryTree()
        {
            DataContext = new DirectoryTreeViewModel();
            InitializeComponent();
        }

        private void Expander_OnExpanded(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Control ctrl &&
                ctrl.DataContext is DirectoryViewModel vm)
                vm.Open();
        }

        private void Expander_OnCollapsed(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Control ctrl &&
                ctrl.DataContext is DirectoryViewModel vm)
                vm.Close();
        }

        
    }
}
