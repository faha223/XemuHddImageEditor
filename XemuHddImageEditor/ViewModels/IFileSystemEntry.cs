namespace XemuHddImageEditor.ViewModels
{
    public interface IFileSystemEntry
    {
        string Name { get; set; }
        string FullName { get; }
        Task Extract();
        Task Rename();
    }
}
