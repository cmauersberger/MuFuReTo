using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        public string SelectedFolder; // { get; set; }
        public string RenamingString { get; set; }
        public ObservableCollection<MediaFileMetaData> ImageFiles { get; set; } = new ObservableCollection<MediaFileMetaData>();
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
            TxtProgress.Text = TxtRenamingScheme.Text;
            var imageFiles = ImageFiles.ToList();
            Renaming.ApplyNamingTemplate(TxtRenamingScheme.Text, imageFiles);
            ImageFiles.Clear();
            imageFiles.ForEach(ImageFiles.Add);
        }

        private void BtnSomeFunction_OnClick(object sender, RoutedEventArgs e)
        {
            
        }


    }
}
