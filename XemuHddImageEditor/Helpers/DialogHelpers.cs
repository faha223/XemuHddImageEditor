using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace XemuHddImageEditor.Helpers;

public static class DialogHelpers
{    
    static Window? AppMainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static async Task ShowDialog<T>(Window dialog)
    {
        var mainWindow = AppMainWindow;
        if(mainWindow != null)
            await dialog.ShowDialog<T>(mainWindow);
    }

    public static async Task<string?> SaveFileDialog(string filename)
    {
        var mainWindow = AppMainWindow;
        if(mainWindow != null)
        {
            var options = new FilePickerSaveOptions()
            {
            };
            var result = await mainWindow.StorageProvider.SaveFilePickerAsync(options);
            return result?.Path.ToString();
        }

        return null;
    }
    
    public static async Task<string?> OpenFileDialog()
    {
        var mainWindow = AppMainWindow;
        if(mainWindow != null)
        {
            var options = new FilePickerOpenOptions()
            {
                AllowMultiple = false
            };
            var result = await mainWindow.StorageProvider.OpenFilePickerAsync(options);
            if (result != null && result.Count > 0)
                return result[0].Path.LocalPath;
        }

        return null;
    }
    
    public static async Task<string[]?> OpenFilesDialog()
    {
        var mainWindow = AppMainWindow;
        if(mainWindow != null)
        {
            var options = new FilePickerOpenOptions()
            {
                AllowMultiple = true
            };
            var result = await mainWindow.StorageProvider.OpenFilePickerAsync(options);
            return result.Select(c => c.Path.LocalPath.ToString()).ToArray();
        }

        return null;
    }

    public static async Task<string?> OpenFolderDialog()
    {
        var mainWindow = AppMainWindow;
        if(mainWindow != null)
        {
            var options = new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            };
            var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(options);
            return result[0].Path.LocalPath.ToString();
        }

        return null;
    }
}