using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace MuFuReTo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string SelectedFolder = null;
        public List<ImageFile> ImageFiles = new List<ImageFile>();

        public MainWindow()
        {
            InitializeComponent();

        }

        private void BtnOpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();


            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFolder.Text = SelectedFolder = folderBrowserDialog.SelectedPath;
                //SelectedFolder = openFileDialog.FileName;
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

            var files = Directory.GetFiles(SelectedFolder);
            var imageFiles = files.Select(file =>
            {
                var fileInfo = new FileInfo(file);
                return new ImageFile
                {
                    Path = fileInfo.DirectoryName,
                    OriginalFilename = fileInfo.Name,
                    FileSize = fileInfo.Length
                };
            }).ToList();

            ImageFiles = imageFiles;
            dgImageFiles.ItemsSource = ImageFiles; // todo: make auto-binding
        }
    }
}
