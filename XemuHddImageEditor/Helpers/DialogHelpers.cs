using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Helpers;

public static class DialogHelpers
{
    static Interaction<Window, Window?> showDialog =new();
    
    public static async Task ShowDialog<T>(Window dialog)
    {
        var app = Application.Current;
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<T>(desktop.MainWindow);
        }
    }

    public static async Task<string?> SaveFileDialog(string filename)
    {
        var app = Application.Current;
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new SaveFileDialog
            {
                InitialFileName = filename
            };
            return await dialog.ShowAsync(desktop.MainWindow);
        }

        return null;
    }
    
    public static async Task<string?> OpenFileDialog()
    {
        var app = Application.Current;
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new OpenFileDialog()
            {
                AllowMultiple = false
            };
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
                return result[0];
        }

        return null;
    }
    
    public static async Task<string[]?> OpenFilesDialog()
    {
        var app = Application.Current;
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new OpenFileDialog()
            {
                AllowMultiple = true
            };
            return await dialog.ShowAsync(desktop.MainWindow); }

        return null;
    }

    public static async Task<string?> OpenFolderDialog()
    {
        var app = Application.Current;
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new OpenFolderDialog();
            return await dialog.ShowAsync(desktop.MainWindow);
        }

        return null;
    }
}