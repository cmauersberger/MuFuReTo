using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MuFuReTo.Code
{
    public class Renaming
    {
        public void ApplyNamingTemplate(string template, ObservableCollection<MediaFileMetaData> mediaFiles, int midnightThreshold, int firstCounter)
        {
            if (!mediaFiles.Any())
            {
                return;
            }

            var counter = firstCounter;
            var dateTaken = mediaFiles.First(mf => mf.DateTaken.HasValue).DateTaken;
            if (dateTaken == null)
            {
                return;
            }

            var currentDate = dateTaken.Value.AddHours(-midnightThreshold).Date;
            var counterDigits = GetCounterDigits(currentDate, mediaFiles, midnightThreshold);

            foreach (var mediaFile in mediaFiles)
            {
                if (!mediaFile.IncludeInRenaming)
                {
                    continue;
                }

                mediaFile.NewFilenameIsUnique = true;
                var newFilename = ReplaceDateFields(template, mediaFile, midnightThreshold);
                newFilename = ReplaceCustomFields(newFilename, mediaFile);

                if (mediaFile.DateTaken.HasValue &&
                    mediaFile.DateTaken.Value.AddHours(-midnightThreshold).Date > currentDate.Date)
                {
                    currentDate = mediaFile.DateTaken.Value.AddHours(-midnightThreshold).Date;
                    counter = 1;
                    counterDigits = GetCounterDigits(currentDate, mediaFiles, midnightThreshold);
                }

                newFilename = newFilename.Replace("%C", counter.ToString().PadLeft(counterDigits, '0'));
                newFilename = newFilename.Trim();
                var extension = Path.GetExtension(mediaFile.CurrentFilename).ToLowerInvariant();
                mediaFile.NewFilename = Path.ChangeExtension(newFilename, extension);
                counter++;
            }

            CheckForUniqueness(mediaFiles);
        }

        private int GetCounterDigits(DateTime currentDate, ObservableCollection<MediaFileMetaData> mediaFiles, int midnightThreshold)
        {
            var filesPerDay = mediaFiles.Count(mf =>
                mf.DateTaken.HasValue && mf.DateTaken.Value.AddHours(-midnightThreshold).Date == currentDate);

            var digits = filesPerDay.ToString().Length;
            return Math.Max(2, digits);
        }

        private void CheckForUniqueness(ObservableCollection<MediaFileMetaData> mediaFiles)
        {
            var groupedByName = mediaFiles.GroupBy(mf => mf.NewFilename).Where(g => g.Count() > 1);
            groupedByName.ToList().ForEach(group =>
            {
                group.ToList().ForEach(mf => mf.NewFilenameIsUnique = false);
            });
        }

        private string ReplaceDateFields(string template, MediaFileMetaData mediaFile, int midnightThreshold)
        {
            var newFilename = template;
            var dateWithThreshold = mediaFile.DateTaken?.AddHours(-midnightThreshold);

            newFilename = newFilename.Replace("%Y", dateWithThreshold?.Year.ToString());
            newFilename = newFilename.Replace("%M", dateWithThreshold?.Month.ToString().PadLeft(2, '0'));
            newFilename = newFilename.Replace("%D", dateWithThreshold?.Day.ToString().PadLeft(2, '0'));

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

    }
}
