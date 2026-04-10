using ELearningWebsite.Areas.Admin.ViewModels;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class MediaLibraryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrivateBlobStorageService _blobStorageService;
        private readonly BlobStorageSettings _blobSettings;

        public MediaLibraryController(
            ApplicationDbContext context,
            IPrivateBlobStorageService blobStorageService,
            IOptions<BlobStorageSettings> blobSettings)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _blobSettings = blobSettings.Value;
        }

        public async Task<IActionResult> Index(int? folderId = null, int? courseId = null, string search = "")
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Forbid();
            }

            if (folderId.HasValue && !await CanAccessFolderAsync(folderId.Value, currentUserId.Value))
            {
                return Forbid();
            }

            var query = _context.MediaFiles
                .Include(x => x.Course)
                .Include(x => x.Folder)
                .Where(x => x.Status == "Active")
                .AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(x => x.OwnerUserId == currentUserId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(x => x.CourseId == courseId.Value);
            }

            if (folderId.HasValue)
            {
                query = query.Where(x => x.FolderId == folderId.Value);
            }
            else
            {
                query = query.Where(x => x.FolderId == null);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.OriginalFileName.Contains(search));
            }

            var files = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new MediaLibraryListItem
                {
                    Id = x.Id,
                    OriginalFileName = x.OriginalFileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    CreatedAt = x.CreatedAt,
                    OwnerUserId = x.OwnerUserId,
                    CourseId = x.CourseId,
                    CourseName = x.Course != null ? (x.Course.Title ?? "") : string.Empty,
                    FolderId = x.FolderId,
                    FolderName = x.Folder != null ? x.Folder.Name : string.Empty
                })
                .ToListAsync();

            var foldersQuery = _context.MediaFolders
                .Where(f => f.Status == "Active")
                .AsQueryable();

            if (!IsAdmin())
            {
                foldersQuery = foldersQuery.Where(f => f.OwnerUserId == currentUserId.Value);
            }

            if (courseId.HasValue)
            {
                foldersQuery = foldersQuery.Where(f => f.CourseId == courseId.Value);
            }

            foldersQuery = foldersQuery.Where(f => f.ParentFolderId == folderId);

            var folders = await foldersQuery
                .OrderBy(f => f.Name)
                .Select(f => new MediaFolderItem
                {
                    Id = f.Id,
                    Name = f.Name,
                    ParentFolderId = f.ParentFolderId,
                    CreatedAt = f.CreatedAt,
                    FileCount = _context.MediaFiles.Count(m => m.Status == "Active" && m.FolderId == f.Id)
                })
                .ToListAsync();

            var coursesQuery = _context.Courses.AsQueryable();
            if (!IsAdmin())
            {
                coursesQuery = coursesQuery.Where(c => c.CreateBy == currentUserId.Value);
            }

            var vm = new MediaLibraryIndexViewModel
            {
                Files = files,
                Folders = folders,
                SelectedCourseId = courseId,
                SelectedFolderId = folderId,
                Search = search,
                TotalBytes = files.Sum(x => x.SizeBytes),
                AvailableCourses = await coursesQuery
                    .OrderBy(c => c.Title)
                    .ToListAsync()
            };

            vm.Breadcrumbs = await BuildBreadcrumbsAsync(folderId, currentUserId.Value);
            vm.CurrentFolderName = vm.Breadcrumbs.LastOrDefault()?.Name ?? "Root";
            ViewData["BlobConfigured"] = IsBlobConfigured();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(string folderName, int? parentFolderId, int? courseId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                TempData["ErrorMessage"] = "Tên thư mục không được để trống.";
                return RedirectToAction(nameof(Index), new { folderId = parentFolderId, courseId });
            }

            if (parentFolderId.HasValue && !await CanAccessFolderAsync(parentFolderId.Value, currentUserId.Value))
            {
                return Forbid();
            }

            if (courseId.HasValue)
            {
                var canUseCourse = await _context.Courses
                    .AnyAsync(c => c.Id == courseId.Value && (IsAdmin() || c.CreateBy == currentUserId.Value));
                if (!canUseCourse)
                {
                    return Forbid();
                }
            }

            var normalizedName = folderName.Trim();
            var exists = await _context.MediaFolders.AnyAsync(f =>
                f.Status == "Active"
                && f.OwnerUserId == currentUserId.Value
                && f.ParentFolderId == parentFolderId
                && f.Name == normalizedName);

            if (exists)
            {
                TempData["ErrorMessage"] = "Thư mục đã tồn tại trong cấp này.";
                return RedirectToAction(nameof(Index), new { folderId = parentFolderId, courseId });
            }

            _context.MediaFolders.Add(new MediaFolder
            {
                Name = normalizedName,
                ParentFolderId = parentFolderId,
                OwnerUserId = currentUserId.Value,
                CourseId = courseId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo thư mục thành công.";
            return RedirectToAction(nameof(Index), new { folderId = parentFolderId, courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, int? courseId, int? folderId)
        {
            if (!IsBlobConfigured())
            {
                TempData["ErrorMessage"] = "BlobStorage:ConnectionString chưa được cấu hình. Vui lòng cấu hình trước khi upload.";
                return RedirectToAction(nameof(Index), new { folderId, courseId });
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Forbid();
            }

            if (file == null)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file để upload.";
                return RedirectToAction(nameof(Index), new { folderId, courseId });
            }

            if (folderId.HasValue && !await CanAccessFolderAsync(folderId.Value, currentUserId.Value))
            {
                return Forbid();
            }

            if (courseId.HasValue)
            {
                var canUseCourse = await _context.Courses
                    .AnyAsync(c => c.Id == courseId.Value && (IsAdmin() || c.CreateBy == currentUserId.Value));

                if (!canUseCourse)
                {
                    return Forbid();
                }
            }

            try
            {
                var upload = await _blobStorageService.UploadAsync(file, currentUserId.Value);

                var entity = new MediaFile
                {
                    OriginalFileName = Path.GetFileName(file.FileName),
                    BlobName = upload.BlobName,
                    BlobPath = upload.BlobPath,
                    ContentType = upload.ContentType,
                    SizeBytes = upload.SizeBytes,
                    OwnerUserId = currentUserId.Value,
                    CourseId = courseId,
                    FolderId = folderId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MediaFiles.Add(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Upload file thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Upload thất bại: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { folderId, courseId });
        }

        public async Task<IActionResult> Open(int id)
        {
            var file = await _context.MediaFiles.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Active");
            if (file == null)
            {
                return NotFound();
            }

            if (!await CanAccessMediaFileAsync(file))
            {
                return Forbid();
            }

            try
            {
                var url = await _blobStorageService.GenerateReadUrlAsync(file.BlobName);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể mở file: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _context.MediaFiles.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Active");
            if (file == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy file.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanAccessMediaFileAsync(file))
            {
                return Forbid();
            }

            try
            {
                await _blobStorageService.DeleteIfExistsAsync(file.BlobName);
                file.Status = "Deleted";
                file.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã xóa file.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Xóa file thất bại: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var id))
            {
                return id;
            }

            return null;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        private async Task<bool> CanAccessMediaFileAsync(MediaFile mediaFile)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            if (mediaFile.OwnerUserId == currentUserId.Value)
            {
                return true;
            }

            if (mediaFile.CourseId.HasValue)
            {
                return await _context.Courses
                    .AnyAsync(c => c.Id == mediaFile.CourseId.Value && c.CreateBy == currentUserId.Value);
            }

            return false;
        }

        private async Task<bool> CanAccessFolderAsync(int folderId, int currentUserId)
        {
            if (IsAdmin())
            {
                return await _context.MediaFolders.AnyAsync(f => f.Id == folderId && f.Status == "Active");
            }

            return await _context.MediaFolders.AnyAsync(f =>
                f.Id == folderId
                && f.Status == "Active"
                && f.OwnerUserId == currentUserId);
        }

        private async Task<List<MediaFolderBreadcrumbItem>> BuildBreadcrumbsAsync(int? folderId, int currentUserId)
        {
            var crumbs = new List<MediaFolderBreadcrumbItem>
            {
                new MediaFolderBreadcrumbItem { Id = null, Name = "Root" }
            };

            var guard = 0;
            var currentId = folderId;

            while (currentId.HasValue && guard < 30)
            {
                var folder = await _context.MediaFolders
                    .FirstOrDefaultAsync(f => f.Id == currentId.Value && f.Status == "Active");

                if (folder == null)
                {
                    break;
                }

                if (!IsAdmin() && folder.OwnerUserId != currentUserId)
                {
                    break;
                }

                crumbs.Add(new MediaFolderBreadcrumbItem
                {
                    Id = folder.Id,
                    Name = folder.Name
                });

                currentId = folder.ParentFolderId;
                guard++;
            }

            crumbs = crumbs.Take(1).Concat(crumbs.Skip(1).Reverse()).ToList();
            return crumbs;
        }

        [HttpGet]
        public async Task<IActionResult> Browse(int? folderId = null, int? courseId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            if (folderId.HasValue && !await CanAccessFolderAsync(folderId.Value, currentUserId.Value))
            {
                return Forbid();
            }

            var foldersQuery = _context.MediaFolders
                .Where(f => f.Status == "Active" && f.ParentFolderId == folderId)
                .AsQueryable();

            if (!IsAdmin())
            {
                foldersQuery = foldersQuery.Where(f => f.OwnerUserId == currentUserId.Value);
            }

            if (courseId.HasValue)
            {
                foldersQuery = foldersQuery.Where(f => f.CourseId == courseId.Value);
            }

            var folders = await foldersQuery
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    itemCount = _context.MediaFiles.Count(m => m.Status == "Active" && m.FolderId == f.Id)
                })
                .ToListAsync();

            var query = _context.MediaFiles
                .Where(x => x.Status == "Active" && x.ContentType.StartsWith("video/"))
                .AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(x => x.OwnerUserId == currentUserId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(x => x.CourseId == courseId.Value);
            }

            if (folderId.HasValue)
            {
                query = query.Where(x => x.FolderId == folderId.Value);
            }
            else
            {
                query = query.Where(x => x.FolderId == null);
            }

            var videos = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.OriginalFileName,
                    size = x.SizeBytes,
                    sizeDisplay = Math.Round(x.SizeBytes / 1024.0 / 1024.0, 2).ToString() + " MB",
                    createdAt = x.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    selectUrl = Url.Action("Open", "Media", new { area = "", id = x.Id })
                })
                .ToListAsync();

            var breadcrumbs = await BuildBreadcrumbsAsync(folderId, currentUserId.Value);

            return Json(new
            {
                currentFolderId = folderId,
                breadcrumbs = breadcrumbs.Select(x => new { id = x.Id, name = x.Name }),
                folders,
                videos
            });
        }

        private bool IsBlobConfigured()
        {
            return !string.IsNullOrWhiteSpace(_blobSettings.ConnectionString);
        }
    }
}