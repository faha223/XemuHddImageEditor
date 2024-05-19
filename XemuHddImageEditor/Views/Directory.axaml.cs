using System.Security.Cryptography;
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
    }
}
