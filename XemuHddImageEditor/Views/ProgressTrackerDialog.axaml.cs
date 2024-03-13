using Avalonia.Controls;
using Avalonia.Threading;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views
{
    public partial class ProgressTrackerDialog : Window
    {
        public ProgressTrackerViewModel ViewModel;

        public ProgressTrackerDialog() : this(new Dictionary<string, Action>())
        {
        }
        
        public ProgressTrackerDialog(Dictionary<string, Action> tasks)
        {
            InitializeComponent();

            ViewModel = new ProgressTrackerViewModel(tasks);
            ViewModel.CloseRequested += Vm_CloseRequested;
            DataContext = ViewModel;
        }

        private void Vm_CloseRequested(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ViewModel.CloseRequested -= Vm_CloseRequested;
                Close();
            });
        }
    }
}
