using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using ExifLibrary;

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

            files.ForEach(file =>
            {
                var fileInfo = new FileInfo(file);
                var img = ImageFile.FromFile(file);
                var isoTag = img.Properties.Get<ExifUShort>(ExifTag.ISOSpeedRatings); // https://github.com/oozcitak/exiflibrary
                var cameraModel = img.Properties.Get<ExifAscii>(ExifTag.Model);

                var imageFile = new ImageFileMetaData
                {
                    Path = fileInfo.DirectoryName,
                    OriginalFilename = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    IsoValue = isoTag.Value,
                    CameraModel = cameraModel.Value
                };

                ImageFiles.Add(imageFile);
            });

        }
    }
}
