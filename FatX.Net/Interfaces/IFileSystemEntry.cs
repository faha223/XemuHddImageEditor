namespace FatX.Net.Interfaces
{
    public interface IFileSystemEntry
    {
        // Setting the Name will rename the file/directory
        string Name { get; set; }
        string FullName { get; }
        Directory? Parent { get; }

        Task Delete();
        Task Extract(string destination);
    }
}
