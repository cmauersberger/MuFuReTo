using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using MuFuReTo.Code;

namespace MuFuReTo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        public string SelectedFolder;
        public ObservableCollection<MediaFileMetaData> ImageFiles = new ObservableCollection<MediaFileMetaData>();
        public MediaFileParser MediaFileParser = new MediaFileParser();

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
                ImageFiles.Clear();
                var imageFiles = MediaFileParser.ReadAllFiles(SelectedFolder);
                imageFiles.ForEach(ImageFiles.Add);
            }
        }

        private void BtnSomeFunction_OnClick(object sender, RoutedEventArgs e)
        {
            
        }


    }
}
