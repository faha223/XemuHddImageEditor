namespace XemuHddImageEditor.ViewModels
{
    public class ProgressTrackerViewModel : ViewModelBase
    {
        private const int DelayBeforeAutoClose = 500; // Milliseconds

        private int _numTasksCompleted = 0;
        public int NumTasksCompleted 
        {
            get => _numTasksCompleted;
            set
            {
                _numTasksCompleted = value;
                OnPropertyChanged(nameof(NumTasksCompleted));
                OnPropertyChanged(nameof(CurrentTask));
            }
        }

        public string CurrentTask => NumTasksCompleted < Tasks.Count ? Tasks.Keys.ToArray()[NumTasksCompleted] : "Done.";

        public Dictionary<string, Action> Tasks { get; init; }

        public ProgressTrackerViewModel()
        {
            // This is debug data
            Tasks = new Dictionary<string, Action>
            {
                { "Extracting C:\\Audio.xbe", () => { } },
                { "Extracting C:\\xboxdash.xbe", () => { } }
            };
            NumTasksCompleted = 1;
        }

        public event EventHandler? CloseRequested;

        public ProgressTrackerViewModel(Dictionary<string, Action> tasks)
        {
            Tasks = tasks;
            _ = RunAllTheTasks();
        }

        private async Task RunAllTheTasks()
        {
            await Task.Run(async () =>
            {
                NumTasksCompleted = 0;
                foreach (var task in Tasks)
                {
                    task.Value.Invoke();
                    NumTasksCompleted++;
                }
                await Task.Delay(DelayBeforeAutoClose);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
