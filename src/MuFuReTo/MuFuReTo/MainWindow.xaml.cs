using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Directory = System.IO.Directory;

namespace MuFuReTo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        public string SelectedFolder;
        public ObservableCollection<ImageFileMetaData> ImageFiles = new ObservableCollection<ImageFileMetaData>();

        public MainWindow()
        {
            InitializeComponent();
            var dgImageFilesDataSource = (CollectionViewSource)(FindResource("DgImageFilesDataSource"));
            dgImageFilesDataSource.Source = ImageFiles;

        }

        private void BtnOpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();


            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFolder.Text = SelectedFolder = folderBrowserDialog.SelectedPath;
                ReadAllFiles();
            }
        }

        private void ReadAllFiles()
        {
            if (string.IsNullOrWhiteSpace(SelectedFolder))
            {
                return;
            }

            if (!Directory.Exists(SelectedFolder))
            {
                return;
            }

            var files = Directory.GetFiles(SelectedFolder).ToList();
            ImageFiles.Clear();

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                try
                {
                    var metadataExtractorDirectories = ImageMetadataReader.ReadMetadata(file); // very fast! 

                    //Alternative:
                    //var et = new ExifTool(new ExifToolOptions());
                    //var exifToolList = (await et.GetTagsAsync(file)).ToList(); https://github.com/AerisG222/NExifTool
                    //var cameraModel = exifToolList.SingleOrDefaultPrimaryTag("Model").Value;


                    //Alternative (much slower, but for writing?)
                    //using ExifLibrary
                    //var img = ImageFile.FromFile(file);
                    //var isoTag = img.Properties.Get<ExifUShort>(ExifTag.ISOSpeedRatings).Value; // https://github.com/oozcitak/exiflibrary
                    //var cameraModel = img.Properties.Get<ExifAscii>(ExifTag.Model).Value;


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

                    var isMp4 = file.EndsWith("mp4", StringComparison.OrdinalIgnoreCase);
                    if (isMp4)
                    {
                        // https://github.com/drewnoakes/metadata-extractor-images/blob/master/mp4/metadata/dotnet/sample_mpeg4.mp4.txt
                        // QuickTime Movie/Track Header: width/height/Created
                        var quickTimeMovieHeader = metadataExtractorDirectories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
                        dateTimeTakenString = quickTimeMovieHeader?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated);

                        var quickTimeTrackHeaderLast = metadataExtractorDirectories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault();
                        widthInPixel = quickTimeTrackHeaderLast?.GetDescription(QuickTimeTrackHeaderDirectory.TagWidth);
                        heightInPixel = quickTimeTrackHeaderLast?.GetDescription(QuickTimeTrackHeaderDirectory.TagHeight);
                    }

                    var imageFile = new ImageFileMetaData
                    {
                        FilePath = fileInfo.DirectoryName,
                        OriginalFilename = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        CameraModel = cameraModel,
                        Width = widthInPixel,
                        Height = heightInPixel,
                        IsoValue = isoSpeed,
                        ExposureTime = exposureTime,
                        FocalLength = focalLength,
                        Aperture = aperture,
                        DateTakenString = dateTimeTakenString,
                        DateTaken = isMp4 ? ParseMp4Date(dateTimeTakenString) : ParseExifDate(dateTimeTakenString)
                    };

                    ImageFiles.Add(imageFile);
                    TxtProgress.Text = $"Processed {ImageFiles.Count} / {files.Count}";
                    foreach (var directory in metadataExtractorDirectories)
                    foreach (var tag in directory.Tags)
                        Console.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");
                }
                catch (Exception e)
                {
                    var imageFile = new ImageFileMetaData
                    {
                        FilePath = fileInfo.DirectoryName,
                        OriginalFilename = fileInfo.Name,
                        FileSize = 0,
                        ParsingRemarks = $"Error: {e.Message}",
                    };

                    ImageFiles.Add(imageFile);

                    Console.WriteLine(e);
                }
            }

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
    }
}
