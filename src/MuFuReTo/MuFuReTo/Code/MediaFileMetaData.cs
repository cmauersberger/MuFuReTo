using System;
using System.IO;

namespace MuFuReTo.Code
{

    public class MediaFileMetaData
    {
        public bool Selected { get; set; } = true;
        public string FilePath { get; set; }
        public string CurrentFilename { get; set; }
        public string NewFilename { get; set; }
        public bool NewFilenameIsUnique { get; set; }
        public string CustomField1 { get; set; }

        public long FileSize { get; set; }
        public FileTypeEnum FileType { get; set; }
        public string FileTypeFormatted => FileType.ToString();
        public string DateTakenString { get; set; }
        public DateTime? DateTaken { get; set; }

        public string DateTakenFormatted => DateTaken.HasValue
            ? DateTaken.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : $"- ({DateTakenString})";

        public string CameraModel { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        public string IsoValue { get; set; }
        public string ExposureTime { get; set; }
        public string FocalLength { get; set; }
        public string Aperture { get; set; }

        public string ParsingRemarks { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                var fullPath = Path.Combine(FilePath, CurrentFilename);
                if (!File.Exists(fullPath))
                {
                    return "";
                }

                string[] sizes = {"B", "KB", "MB", "GB", "TB"};
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
}
