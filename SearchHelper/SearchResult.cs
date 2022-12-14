namespace Community.PowerToys.Run.Plugin.Everything
{
    using Wox.Plugin.Interfaces;

    public class SearchResult : IFileDropResult
    {
        // Contains the Path of the file or folder
        public string Path { get; set; }

        // Contains the Title of the file or folder
        public string Title { get; set; }

        // States if result is a file
        public bool File { get; set; }
    }
}
