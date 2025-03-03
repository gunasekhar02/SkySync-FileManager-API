namespace SkySync_FileManager_API.Models
{
    public class FileInfo
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; } // e.g., "image", "video", "pdf", "audio"
        public string FilePath { get; set; } // Path in Dropbox
        public DateTime UploadDate { get; set; }

        public long FileSize { get; set; } 
    }
}
