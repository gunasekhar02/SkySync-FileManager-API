using Dropbox.Api.Files;
using Dropbox.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkySync_FileManager_API.Models;
using FileInfo = SkySync_FileManager_API.Models.FileInfo;
using Microsoft.EntityFrameworkCore;

namespace SkySync_FileManager_API.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FileController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public FileController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Upload a file
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileType = GetFileType(file.ContentType);
            var filePath = await UploadToDropbox(file);

            var fileInfo = new FileInfo
            {
                UserId = userId,
                FileName = file.FileName,
                FileType = fileType,
                FilePath = filePath,
                FileSize = file.Length,
                UploadDate = DateTime.UtcNow
            };

            _context.FileInfos.Add(fileInfo);
            await _context.SaveChangesAsync();

            return Ok(new { fileInfo.Id });
        }

        // Get all files for the logged-in user
        [HttpGet]
        public async Task<IActionResult> GetFiles()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var files = await _context.FileInfos
                .Where(f => f.UserId == userId)
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileType,
                    f.FilePath,
                    FileSize = GetReadableFileSize(f.FileSize),
                    f.UploadDate
                })
                .ToListAsync();

            return Ok(files);
        }

        // Download a file
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fileInfo = await _context.FileInfos.FindAsync(id);

            if (fileInfo == null || fileInfo.UserId != userId)
                return Unauthorized("user doesnot have access");

            var fileBytes = await DownloadFromDropbox(fileInfo.FilePath);
            return File(fileBytes, "application/octet-stream", fileInfo.FileName);
        }

        // Helper method to determine file type
        private string GetFileType(string contentType)
        {
            if (contentType.StartsWith("image"))
                return "image";
            else if (contentType.StartsWith("video"))
                return "video";
            else if (contentType == "application/pdf")
                return "pdf";
            else if (contentType.StartsWith("audio"))
                return "audio";
            else
                return "other";
        }

        // Helper method to upload file to Dropbox
        private async Task<string> UploadToDropbox(IFormFile file)
        {
            var accessToken = _config["Dropbox:AccessToken"];
            var dropboxClient = new DropboxClient(accessToken);

            using (var stream = file.OpenReadStream())
            {
                var response = await dropboxClient.Files.UploadAsync(
                    $"/{Guid.NewGuid()}_{file.FileName}", // Unique file name
                    WriteMode.Overwrite.Instance,
                    body: stream);

                return response.PathDisplay;
            }
        }

        // Helper method to download file from Dropbox
        private async Task<byte[]> DownloadFromDropbox(string filePath)
        {
            var accessToken = _config["Dropbox:AccessToken"];
            var dropboxClient = new DropboxClient(accessToken);

            var response = await dropboxClient.Files.DownloadAsync(filePath);
            using (var stream = await response.GetContentAsStreamAsync())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }

        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest("File path is required.");
            }

            try
            {
                var accessToken = _config["Dropbox:AccessToken"];
                using (var dropboxClient = new DropboxClient(accessToken))
                {
                    // Delete the file
                    await dropboxClient.Files.DeleteV2Async(filePath);
                    var fileInfo = await _context.FileInfos
                        .FirstOrDefaultAsync(f => f.FilePath == filePath);

                    if (fileInfo != null)
                    {
                        _context.FileInfos.Remove(fileInfo);
                        await _context.SaveChangesAsync();
                    }

                    return Ok($"File '{filePath}' deleted successfully from Dropbox and SQL.");
                }

            }
            catch (Dropbox.Api.ApiException<DeleteError> ex)
            {
                return StatusCode(500, $"Error deleting file: {ex.ErrorResponse}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private static string GetReadableFileSize(long sizeInBytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (sizeInBytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                sizeInBytes /= 1024;
            }
            return $"{sizeInBytes:0.##} {sizes[order]}";
        }
    }
}
