using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ExifLibrary;
using MuFuReTo.Code;
using Image = System.Windows.Controls.Image;

// todo: set exif author from custom field
// todo: set exif date from filename/date picker

namespace MuFuReTo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        public string SelectedFolder; // { get; set; }
        public string RenamingString { get; set; }
        public ObservableCollection<MediaFileMetaData> ImageFiles { get; set; } = new ObservableCollection<MediaFileMetaData>();
        public MediaFileMetaData SelectedCustomer { get; set; }
        public MediaFileParser MediaFileParser = new MediaFileParser();
        public Renaming Renaming = new Renaming();

        public MainWindow()
        {
            InitializeComponent();
            // use this example to bind list, maybe: https://stackoverflow.com/questions/1725554/wpf-simple-textbox-data-binding
            var dgImageFilesDataSource = (CollectionViewSource)(FindResource("DgImageFilesDataSource"));
            dgImageFilesDataSource.Source = ImageFiles;

            //System.Windows.Data.Binding binding = new System.Windows.Data.Binding("Text");
            //binding.Source = TxtRenamingScheme2;
            //RenamingString.SetBinding(TextBlock.TextProperty, binding);

            //LeftPanelGrid.DataContext = this;

        }

        private void BtnOpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFolder.Text = SelectedFolder = folderBrowserDialog.SelectedPath;
                ImageFiles.Clear();
                var imageFiles = MediaFileParser.ReadAllFiles(SelectedFolder);
                imageFiles.ForEach(ImageFiles.Add);
            }
        }

        private void BtnApplyNamingScheme_OnClick(object sender, RoutedEventArgs e)
        {
            Renaming.ApplyNamingTemplate(TxtRenamingScheme.Text, ImageFiles);
            DgImageFiles.Items.Refresh();
        }

        private void BtnExecuteRenaming_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var mediaFile in ImageFiles)
            {
                if (!mediaFile.IncludeInRenaming)
                {
                    continue;
                }

                try
                {
                    var oldPath = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                    var newPath = Path.Combine(mediaFile.FilePath, mediaFile.NewFilename);
                    File.Move(oldPath, newPath);
                    var originalFilename = mediaFile.CurrentFilename;
                    mediaFile.CurrentFilename = mediaFile.NewFilename;
                    mediaFile.NewFilename = originalFilename;
                }
                catch (Exception exception)
                {
                    mediaFile.ParsingRemarks += "Failed with renaming. Error: " + exception.Message;
                }
            }
            DgImageFiles.Items.Refresh();
        }

        private void BtnCopyF1ToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaFiles = DgImageFiles.SelectedItems;

            if (mediaFiles.Count < 1)
            {
                return;
            }

            var sourceString = TxtFillCustomField1.Text;

            foreach (var mediaFile in mediaFiles)
            {
                ((MediaFileMetaData) mediaFile).CustomField1 = sourceString;
            }

            DgImageFiles.Items.Refresh();
        }

        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            if (!CbShowPreview.IsChecked.HasValue || !CbShowPreview.IsChecked.Value)
            {
                ImgPreview.Source = null;
                return;
            }

            if (e.OriginalSource.GetType() != typeof(DataGridCell))
            {
                return;
            }

            DataGrid grd = (DataGrid)sender;
            grd.BeginEdit(e);

            var mediaFile = (MediaFileMetaData)grd.CurrentItem;
            if (mediaFile == null || mediaFile.FileType != FileTypeEnum.Jpg)
            {
                ImgPreview.Source = null;
                return;
            }
            try
            {
                if (mediaFile.CurrentFilename == Path.GetFileName(ImgPreview.Source?.ToString()))
                {
                    return;
                }

                var path = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                // open image as stream, so it is not locket. 
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(path);
                bi.CacheOption = BitmapCacheOption.OnLoad; // This does the trick for file not being locked.
                bi.DecodePixelWidth = 150; // reduce resolution, loads much faster. 12MB at full resolution take 700+ms, with 150 it's only 170ms.
                bi.EndInit(); // this takes time

                ImgPreview.Source = bi;

            }
            catch
            {
                ImgPreview.Source = null;
            }
        }

        private void BtnCopyCopyrightToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaFiles = DgImageFiles.SelectedItems;

            if (mediaFiles.Count < 1)
            {
                return;
            }

            var copyright = ComboBoxCopyright.Text;

            foreach (var mediaFileObject in mediaFiles)
            {
                var mediaFile = (MediaFileMetaData)mediaFileObject;

                if (mediaFile.FileType != FileTypeEnum.Jpg)
                {
                    continue;
                }

                // https://github.com/oozcitak/exiflibrary (writing exif data)
                var fullPath = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                var file = ImageFile.FromFile(fullPath);
                file.Properties.Set(ExifTag.Copyright, copyright);
                mediaFile.Copyright = copyright;
                file.Save(fullPath);
            }

            DgImageFiles.Items.Refresh();
        }

        private void BtnCopyDateTakenToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaFiles = DgImageFiles.SelectedItems;

            if (mediaFiles.Count < 1)
            {
                return;
            }

            // todo: add checkbox for adding x minutes for each picture
            // Example: "2020:05:12 12:24:59"
            var dateTakenString = TxtFillDateTaken.Text;
            var dateTaken = DateTime.ParseExact(dateTakenString, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);

            foreach (var mediaFileObject in mediaFiles)
            {
                var mediaFile = (MediaFileMetaData)mediaFileObject;
                try
                {
                    // https://github.com/oozcitak/exiflibrary (writing exif data)

                    if (mediaFile.FileType != FileTypeEnum.Jpg)
                    {
                        continue;
                    }

                    var fullPath = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                    var file = ImageFile.FromFile(fullPath);
                    file.Properties.Set(ExifTag.DateTimeOriginal, dateTaken);
                    file.Save(fullPath);
                    mediaFile.DateTakenString = dateTakenString;
                    mediaFile.DateTaken = dateTaken;
                }
                catch (Exception exception)
                {
                    mediaFile.ParsingRemarks += "Error writing date: " + exception.Message;
                }
            }

            DgImageFiles.Items.Refresh();
        }

        private void BtnCopyNameAsF1ToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaFiles = DgImageFiles.SelectedItems;

            if (mediaFiles.Count < 1)
            {
                return;
            }

            // todo: without first word (as options?)
            foreach (var mediaFile in mediaFiles)
            {
                var currentFilename = Path.GetFileNameWithoutExtension(((MediaFileMetaData)mediaFile).CurrentFilename);
                var pattern = @"\(\d+\)$";
                var output = Regex.Replace(currentFilename, pattern, " ");
                ((MediaFileMetaData)mediaFile).CustomField1 = output;
            }

            DgImageFiles.Items.Refresh();
        }
    }
}
