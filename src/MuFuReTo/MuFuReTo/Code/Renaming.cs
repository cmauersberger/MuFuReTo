using System;
using System.Collections.Generic;
using System.Linq;

namespace MuFuReTo.Code
{
    public class Renaming
    {
        public void ApplyNamingTemplate(string template, List<MediaFileMetaData> mediaFiles)
        {
            var counter = 1;
            var currentDate = new DateTime();
            var counterDigits = 2;
            foreach (var mediaFile in mediaFiles)
            {
                mediaFile.NewFilenameIsUnique = true;
                var newFilename = ReplaceDateFields(template, mediaFile);
                if (mediaFile.DateTaken.HasValue && mediaFile.DateTaken.Value.Date > currentDate.Date)
                {
                    currentDate = mediaFile.DateTaken.Value.Date;
                    counter = 1;
                    var filesPerDay = mediaFiles.Count(mf => mf.DateTaken.HasValue && mf.DateTaken.Value.Date == mediaFile.DateTaken.Value.Date);
                    counterDigits = GetCounterDigits(filesPerDay);
                }

                newFilename = newFilename.Replace("%C", counter.ToString().PadLeft(counterDigits, '0'));
                mediaFile.NewFilename = newFilename;
                counter++;
            }
            CheckForUniqueness(mediaFiles);
        }

        private void CheckForUniqueness(List<MediaFileMetaData> mediaFiles)
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
                newFilename = newFilename.Replace("%M", mediaFile.DateTaken?.Month.ToString().PadLeft(2, '0' ));
            }
            newFilename = newFilename.Replace("%D", mediaFile.DateTaken?.Day.ToString().PadLeft(2, '0'));

            return newFilename;
        }

        private int GetCounterDigits(int filesPerDay)
        {
            var digits = filesPerDay.ToString().Length;
            return Math.Max(2, digits);
        }
    }
}
