using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SmartLearning.Api.Services
{
    // The result of an upload.
    public class UploadedFile
    {
        public string Url { get; set; }

        // Needed later to delete the file.
        public string PublicId { get; set; }

        // Only filled for videos uploaded to Cloudinary.
        public int DurationSeconds { get; set; }
    }

    // Uploads images and videos.
    //
    // - If Cloudinary keys are set in appsettings.json, files go to Cloudinary (free plan).
    // - If they are NOT set, files are saved in the wwwroot/uploads folder instead,
    //   so you can try the project before creating a Cloudinary account.
    public class FileStorageService
    {
        private const string LocalPrefix = "local:";

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".webm", ".mkv", ".avi" };

        private readonly Cloudinary _cloudinary; // stays null when Cloudinary is not configured
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;

            // These values come from appsettings.json -> "Cloudinary" section.
            string cloudName = configuration["Cloudinary:CloudName"];
            string apiKey = configuration["Cloudinary:ApiKey"];
            string apiSecret = configuration["Cloudinary:ApiSecret"];

            if (IsRealValue(cloudName) && IsRealValue(apiKey) && IsRealValue(apiSecret))
            {
                _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
                _cloudinary.Api.Secure = true; // always return https links
            }
        }

        public bool IsCloudinaryConfigured
        {
            get { return _cloudinary != null; }
        }

        public async Task<UploadedFile> UploadImageAsync(IFormFile file)
        {
            CheckFile(file, ImageExtensions);

            if (_cloudinary == null)
            {
                return await SaveLocallyAsync(file, "images");
            }

            using (Stream stream = file.OpenReadStream())
            {
                ImageUploadParams uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "smartlearn/images"
                };

                ImageUploadResult result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    throw new Exception("Cloudinary upload failed: " + result.Error.Message);
                }

                return new UploadedFile
                {
                    Url = result.SecureUrl.ToString(),
                    PublicId = result.PublicId
                };
            }
        }

        public async Task<UploadedFile> UploadVideoAsync(IFormFile file)
        {
            CheckFile(file, VideoExtensions);

            if (_cloudinary == null)
            {
                return await SaveLocallyAsync(file, "videos");
            }

            using (Stream stream = file.OpenReadStream())
            {
                VideoUploadParams uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "smartlearn/videos"
                };

                // UploadLarge sends the video in 20 MB pieces, which is more reliable for big files.
                // Note: the Cloudinary FREE plan accepts videos up to 100 MB.
                VideoUploadResult result = await _cloudinary.UploadLargeAsync(uploadParams, 20 * 1024 * 1024);
                if (result.Error != null)
                {
                    throw new Exception("Cloudinary upload failed: " + result.Error.Message);
                }

                return new UploadedFile
                {
                    Url = result.SecureUrl.ToString(),
                    PublicId = result.PublicId,
                    DurationSeconds = (int)Math.Round(result.Duration)
                };
            }
        }

        // Deletes an old file (for example when an instructor replaces a lecture video).
        public async Task DeleteFileAsync(string publicId, bool isVideo)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            if (publicId.StartsWith(LocalPrefix))
            {
                string relativePath = publicId.Substring(LocalPrefix.Length);
                string fullPath = Path.Combine(_environment.WebRootPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                return;
            }

            if (_cloudinary != null)
            {
                DeletionParams deletionParams = new DeletionParams(publicId);
                if (isVideo)
                {
                    deletionParams.ResourceType = ResourceType.Video;
                }
                await _cloudinary.DestroyAsync(deletionParams);
            }
        }

        // ---------- helpers ----------

        private static bool IsRealValue(string value)
        {
            // Empty values and the "YOUR_..." placeholders count as "not configured".
            return !string.IsNullOrWhiteSpace(value) && !value.StartsWith("YOUR_");
        }

        private static void CheckFile(IFormFile file, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception("Please choose a file.");
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("File type " + extension + " is not allowed. Allowed: " + string.Join(", ", allowedExtensions));
            }
        }

        private async Task<UploadedFile> SaveLocallyAsync(IFormFile file, string folderName)
        {
            string folder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLower();
            string fullPath = Path.Combine(folder, fileName);

            using (FileStream output = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(output);
            }

            // Build a full link like http://localhost:5000/uploads/videos/abc.mp4
            HttpRequest request = _httpContextAccessor.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host + "/uploads/" + folderName + "/" + fileName;

            return new UploadedFile
            {
                Url = url,
                PublicId = LocalPrefix + Path.Combine("uploads", folderName, fileName)
            };
        }
    }
}
