using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Directory = System.IO.Directory;

namespace MuFuReTo.Code
{
    public class MediaFileParser
    {

        public List<MediaFileMetaData> ReadAllFiles(string selectedFolder)
        {
            var result = new List<MediaFileMetaData>();
            if (string.IsNullOrWhiteSpace(selectedFolder) || !Directory.Exists(selectedFolder))
            {
                return result;
            }

            var files = Directory.GetFiles(selectedFolder).ToList();

            foreach (var file in files)
            {
                var metaData = new MediaFileMetaData();

                try
                {
                    var fileInfo = new FileInfo(file);
                    metaData.FilePath = fileInfo.DirectoryName;
                    metaData.CurrentFilename = fileInfo.Name;
                    metaData.FileSize = fileInfo.Length;

                    metaData.FileType = GetFileType(file);

                    switch (metaData.FileType)
                    {
                        case FileTypeEnum.Jpg:
                            ParseJpgMetaData(file, metaData);
                            break;
                        case FileTypeEnum.Mp4:
                            ParseMp4MetaData(file, metaData);
                            break;
                        case FileTypeEnum.Undefined:
                            metaData.ExcludeFromRenaming = false;
                            break;
                    }
                }
                catch (Exception e)
                {
                    metaData.ParsingRemarks += $"Error: {e.Message}";
                    Console.WriteLine(e);
                }

                result.Add(metaData);
            }

            return result.OrderBy(mf => mf.DateTaken ?? new DateTime()).ToList();
        }

        private FileTypeEnum GetFileType(string filename)
        {
            var isJpg = filename.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                        filename.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase);

            if (isJpg)
            {
                return FileTypeEnum.Jpg;
            }

            var isMp4 = filename.EndsWith("mp4", StringComparison.OrdinalIgnoreCase);

            if (isMp4)
            {
                return FileTypeEnum.Mp4;
            }

            return FileTypeEnum.Undefined;
        }

        private void ParseJpgMetaData(string filename, MediaFileMetaData metaData)
        {
            var metadataExtractorDirectories = ImageMetadataReader.ReadMetadata(filename); // very fast!

            var exifIfd0Directory = metadataExtractorDirectories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var cameraModel = exifIfd0Directory?.GetDescription(ExifDirectoryBase.TagModel);

            var subIfdDirectory = metadataExtractorDirectories.OfType<ExifSubIfdDirectory>().FirstOrDefault(); // contains image related data
            var exposureTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExposureTime);
            var isoSpeed = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
            var focalLength = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength);
            var dateTimeTakenString = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            var aperture = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagAperture);
            var widthInPixel = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth);
            var heightInPixel = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight);

            metaData.CameraModel = cameraModel;
            metaData.Width = widthInPixel;
            metaData.Height = heightInPixel;
            metaData.IsoValue = isoSpeed;
            metaData.ExposureTime = exposureTime;
            metaData.FocalLength = focalLength;
            metaData.Aperture = aperture;
            metaData.DateTakenString = dateTimeTakenString;
            metaData.DateTaken = ParseExifDate(dateTimeTakenString);
        }

        private void ParseMp4MetaData(string filename, MediaFileMetaData metaData)
        {
            // https://github.com/drewnoakes/metadata-extractor-images/blob/master/mp4/metadata/dotnet/sample_mpeg4.mp4.txt

            var metadataExtractorDirectories = ImageMetadataReader.ReadMetadata(filename); // very fast!

            var quickTimeMovieHeader = metadataExtractorDirectories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            var dateTimeTakenString = quickTimeMovieHeader?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated);

            var quickTimeTrackHeaderLast = metadataExtractorDirectories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault();
            var widthInPixel = quickTimeTrackHeaderLast?.GetDescription(QuickTimeTrackHeaderDirectory.TagWidth);
            var heightInPixel = quickTimeTrackHeaderLast?.GetDescription(QuickTimeTrackHeaderDirectory.TagHeight);

            //metaData.CameraModel = cameraModel;
            metaData.Width = widthInPixel;
            metaData.Height = heightInPixel;
            metaData.DateTakenString = dateTimeTakenString;
            metaData.DateTaken = ParseMp4Date(dateTimeTakenString);
        }

        private DateTime? ParseExifDate(string dateAsString)
        {
            if (string.IsNullOrWhiteSpace(dateAsString))
            {
                return null;
            }
            // Example: "2020:05:12 12:24:59"
            var datePart = dateAsString.Substring(0, dateAsString.IndexOf(" ", StringComparison.Ordinal));
            var timePart = dateAsString.Substring(dateAsString.IndexOf(" ", StringComparison.Ordinal));
            var datePartReplaced = datePart.Replace(":", "-");

            var wasParsed = DateTime.TryParse($"{datePartReplaced} {timePart}", out var dateTimeTaken);
            return wasParsed ? dateTimeTaken : null as DateTime?;
        }

        private DateTime? ParseMp4Date(string dateAsString)
        {
            // example: "Fri Oct 28 17:46:46 2005"

            var dateStringWithoutWeekday = dateAsString.Substring(dateAsString.IndexOf(" ", StringComparison.Ordinal)).Trim(); // remove weekday
            var parts = dateStringWithoutWeekday.Split(" ");
            var month = parts[0];
            var day = parts[1];
            var time = parts[2];
            var year = parts[3];
            var wasParsed = DateTime.TryParse($"{month} {day} {year} {time}", out var dateTimeCreated);
            return wasParsed ? dateTimeCreated : null as DateTime?;
        }


        //Alternative:
        //var et = new ExifTool(new ExifToolOptions());
        //var exifToolList = (await et.GetTagsAsync(file)).ToList(); https://github.com/AerisG222/NExifTool
        //var cameraModel = exifToolList.SingleOrDefaultPrimaryTag("Model").Value;


        //Alternative (much slower, but for writing?)
        //using ExifLibrary
        //var img = ImageFile.FromFile(file);
        //var isoTag = img.Properties.Get<ExifUShort>(ExifTag.ISOSpeedRatings).Value; // https://github.com/oozcitak/exiflibrary
        //var cameraModel = img.Properties.Get<ExifAscii>(ExifTag.Model).Value;


    }
}
