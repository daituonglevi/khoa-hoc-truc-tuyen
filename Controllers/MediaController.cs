using ELearningWebsite.Data;
using ELearningWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrivateBlobStorageService _blobStorageService;

        public MediaController(ApplicationDbContext context, IPrivateBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var media = await _context.MediaFiles
                .FirstOrDefaultAsync(x => x.Id == id && x.Status == "Active");

            if (media == null)
            {
                return NotFound();
            }

            try
            {
                var signedUrl = await _blobStorageService.GenerateReadUrlAsync(media.BlobName);
                return Redirect(signedUrl);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> OpenByBlob(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
            {
                return BadRequest();
            }

            var blobName = ExtractBlobName(blobUrl);
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return NotFound();
            }

            var media = await _context.MediaFiles
                .FirstOrDefaultAsync(x => x.Status == "Active" && x.BlobName == blobName);

            if (media == null)
            {
                return NotFound();
            }

            try
            {
                var signedUrl = await _blobStorageService.GenerateReadUrlAsync(media.BlobName);
                return Redirect(signedUrl);
            }
            catch
            {
                return NotFound();
            }
        }

        private static string ExtractBlobName(string blobUrl)
        {
            var decoded = System.Net.WebUtility.HtmlDecode(blobUrl.Trim());

            if (Uri.TryCreate(decoded, UriKind.Absolute, out var absoluteUri))
            {
                var path = absoluteUri.AbsolutePath.Trim('/');
                const string prefix = "private-media/";
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return path.Substring(prefix.Length);
                }

                return path;
            }

            var normalized = decoded.Replace('\\', '/').Trim('/');
            if (normalized.StartsWith("private-media/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring("private-media/".Length);
            }

            return normalized;
        }
    }
}