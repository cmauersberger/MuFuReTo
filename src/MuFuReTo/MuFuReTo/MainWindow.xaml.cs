using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MuFuReTo.Code;

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
            var image = (MediaFileMetaData)DgImageFiles.SelectedItem;

            if (image == null)
            {
                return;
            }

            try
            {
                var path = Path.Combine(image.FilePath, image.CurrentFilename);
                Uri fileUri = new Uri(path);
                ImgPreview.Source = new BitmapImage(fileUri);

            }
            catch
            {
                // ignored
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
    }
}
