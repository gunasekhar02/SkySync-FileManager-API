using Microsoft.EntityFrameworkCore;


namespace SkySync_FileManager_API.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FileInfo> FileInfos { get; set; }
    }
}
