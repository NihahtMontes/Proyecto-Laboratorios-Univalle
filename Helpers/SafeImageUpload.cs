using Microsoft.AspNetCore.Http;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class SafeImageUpload
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp"
            };

        public static async Task<string?> ValidateAsync(
            IFormFile? file,
            CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                return null;
            }

            if (file.Length <= 0)
            {
                return "El archivo de imagen está vacío.";
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return "La imagen no puede superar los 5 MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType))
            {
                return "La imagen debe ser JPG, PNG o WEBP.";
            }

            if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                return "El tipo de contenido del archivo no coincide con una imagen permitida.";
            }

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            var signatureIsValid = extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3
                    && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8
                    && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => bytesRead >= 12
                    && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                    && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
                _ => false
            };

            return signatureIsValid
                ? null
                : "El contenido del archivo no corresponde a una imagen válida.";
        }

        public static string CreateSafeFileName(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".jpeg")
            {
                extension = ".jpg";
            }

            return $"{Guid.NewGuid():N}{extension}";
        }

        public static async Task<string> SaveAsync(
            IFormFile file,
            string targetFolder,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(targetFolder);
            var fileName = CreateSafeFileName(file);
            var filePath = Path.Combine(targetFolder, fileName);

            await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream, cancellationToken);
            return fileName;
        }

        public static void DeleteIfExists(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public static void DeleteStoredFile(string folder, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var safeFileName = Path.GetFileName(fileName);
            DeleteIfExists(Path.Combine(folder, safeFileName));
        }
    }
}
