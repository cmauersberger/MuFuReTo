using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MuFuReTo.Code;

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
                if (mediaFile.ExcludeFromRenaming)
                {
                    continue;
                }
                var oldPath = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                var newPath = Path.Combine(mediaFile.FilePath, mediaFile.NewFilename);
                var originalFilename = mediaFile.CurrentFilename;
                mediaFile.CurrentFilename = mediaFile.NewFilename;
                mediaFile.NewFilename = originalFilename;
                File.Move(oldPath, newPath);
            }
            DgImageFiles.Items.Refresh();
        }

        private void BtnShowPreview_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaFile = (MediaFileMetaData)DgImageFiles.SelectedItem;

            if (mediaFile == null)
            {
                ImgPreview.Source = null;
                return;
            }

            try
            {
                var path = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                var fileUri = new Uri(path);
                ImgPreview.Source = new BitmapImage(fileUri);

            }
            catch
            {
                ImgPreview.Source = null;

            }
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
                var x = Path.GetFileName(ImgPreview.Source?.ToString());
                if (mediaFile.CurrentFilename == Path.GetFileName(ImgPreview.Source?.ToString()))
                {
                    return;
                }
                var path = Path.Combine(mediaFile.FilePath, mediaFile.CurrentFilename);
                Uri fileUri = new Uri(path);
                ImgPreview.Source = new BitmapImage(fileUri);

            }
            catch
            {
                ImgPreview.Source = null;
            }
        }
    }
}
