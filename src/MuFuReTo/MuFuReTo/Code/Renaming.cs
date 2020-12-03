using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MuFuReTo.Code
{
    public class Renaming
    {
        public void ApplyNamingTemplate(string template, ObservableCollection<MediaFileMetaData> mediaFiles)
        {
            var counter = 1;
            var currentDate = new DateTime();
            var counterDigits = 2;

            foreach (var mediaFile in mediaFiles)
            {
                if (!mediaFile.IncludeInRenaming)
                {
                    continue;
                }

                mediaFile.NewFilenameIsUnique = true;
                var newFilename = ReplaceDateFields(template, mediaFile);
                newFilename = ReplaceCustomFields(newFilename, mediaFile);

                if (mediaFile.DateTaken.HasValue && mediaFile.DateTaken.Value.Date > currentDate.Date)
                {
                    currentDate = mediaFile.DateTaken.Value.Date;
                    counter = 1;
                    var filesPerDay = mediaFiles.Count(mf => mf.DateTaken.HasValue && mf.DateTaken.Value.Date == mediaFile.DateTaken.Value.Date);
                    counterDigits = GetCounterDigits(filesPerDay);
                }

                newFilename = newFilename.Replace("%C", counter.ToString().PadLeft(counterDigits, '0'));
                newFilename = newFilename.Trim();
                var extension = Path.GetExtension(mediaFile.CurrentFilename).ToLowerInvariant();
                mediaFile.NewFilename = Path.ChangeExtension(newFilename, extension);
                counter++;
            }

            CheckForUniqueness(mediaFiles);
        }

        private void CheckForUniqueness(ObservableCollection<MediaFileMetaData> mediaFiles)
        {
            var groupedByName = mediaFiles.GroupBy(mf => mf.NewFilename).Where(g => g.Count() > 1);
            groupedByName.ToList().ForEach(group =>
            {
                group.ToList().ForEach(mf => mf.NewFilenameIsUnique = false);
            });
        }

        private string ReplaceDateFields(string template, MediaFileMetaData mediaFile)
        {
            var newFilename = template;

            if (template.Contains("%Y"))
            {
                newFilename = newFilename.Replace("%Y", mediaFile.DateTaken?.Year.ToString());
            }

            if (template.Contains("%M"))
            {
                newFilename = newFilename.Replace("%M", mediaFile.DateTaken?.Month.ToString().PadLeft(2, '0'));
            }
            newFilename = newFilename.Replace("%D", mediaFile.DateTaken?.Day.ToString().PadLeft(2, '0'));

            return newFilename;
        }

        private string ReplaceCustomFields(string template, MediaFileMetaData mediaFile)
        {
            var newFilename = template;

            if (template.Contains("%F1"))
            {
                newFilename = newFilename.Replace("%F1", mediaFile.CustomField1);
            }

            if (template.Contains("%V"))
            {
                var isVideo = mediaFile.FileType == FileTypeEnum.Mp4 || mediaFile.FileType == FileTypeEnum.Mov;
                newFilename = newFilename.Replace("%V", isVideo ? "v" : "");
            }

            return newFilename;
        }

        private int GetCounterDigits(int filesPerDay)
        {
            var digits = filesPerDay.ToString().Length;
            return Math.Max(2, digits);
        }
    }
}
