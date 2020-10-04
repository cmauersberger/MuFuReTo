using System;
using System.IO;

public class ImageFileMetaData
{
    public string Path { get; set; }
    public string OriginalFilename { get; set; }
    public DateTime DateTaken { get; set; }
    public string CameraModel { get; set; }
    public long FileSize { get; set; }
    public string NewFilenamePreview { get; set; }
    public ushort IsoValue { get; set; }

    public string FormattedFileSize 
    {
        get
        {
            var fullPath = Path + "\\" + OriginalFilename;
            if (!File.Exists(fullPath))
            {
                return "";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(fullPath).Length;
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.#} {sizes[order]}";
        }
    }
}
