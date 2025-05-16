using Microsoft.AspNetCore.Http;

namespace SplitWise.Domain.Generic.Helper;

public static class FileHelper
{
    public static async Task<string> UploadFile(IFormFile file, string path)
    {
        ArgumentNullException.ThrowIfNull(file);

        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

        string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", path);

        CreateFileDirectory(uploadDirectory);

        string filePath = Path.Combine(uploadDirectory, fileName);

        using (FileStream fileStream = new(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/Images/{path}/{fileName}";
    }

    private static void CreateFileDirectory(string uploadDirectory)
    {
        if (!Directory.Exists(uploadDirectory))
        {
            Directory.CreateDirectory(uploadDirectory);
        }
    }

    public static async Task<string> ReadFileFromPath(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    public static async Task<string> ConvertImageToBase64(string imageUrl)
    {
        try
        {
            using HttpClient client = new();
            var imageBytes = await client.GetByteArrayAsync(imageUrl);
            return $"{SplitWiseConstants.IMAGE_SPECIFICATION},{Convert.ToBase64String(imageBytes)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool DeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), SplitWiseConstants.STATIC_FILE_FOLDER, path.TrimStart('/'));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return true;
        }
        return false;
    }

}
