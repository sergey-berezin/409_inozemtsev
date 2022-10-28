using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NuGet_ArcFace_Functions;

namespace WpfApp1
{
    //________________________________ЭЛЕМЕНТ СПИСКА LISTBOX (АБСОЛЮТНЫЙ ПУТЬ К ФАЙЛУ, ЕГО НАЗВАНИЕ И ФОРМАТ)_________________________________
    public class ListItem
    {
        private string path;
        private string name;
        private string ext;
        public string Path { get { return path; } }
        public string Name { get { return name; } }
        public string Ext { get { return ext; } }

        public ListItem(string file_path)
        {
            path = file_path;
            name = System.IO.Path.GetFileName(file_path);
            ext = System.IO.Path.GetExtension(file_path);
        }
    }

    //________________________________________________________ДАННЫЕ ПРИЛОЖЕНИЯ WPF___________________________________________________________
    public class ViewModel : INotifyPropertyChanged
    {
        private string folder_path;
        private string img1;
        private string img2;
        private float distance;
        private float similarity;
        private bool changed;
        private bool cancellable;

        //.........................Абсолютный путь к каталогу с изображениями
        public string FolderPath
        {
            get { return folder_path; }
            set { folder_path = value; OnPropertyChanged("FolderPath"); }
        }

        //.........................Абсолютный путь к первому изображению
        public string Image1
        {
            get { return img1; }
            set { img1 = value; OnPropertyChanged("Image1"); }
        }

        //.........................Абсолютный путь ко второму изображению
        public string Image2
        {
            get { return img2; }
            set { img2 = value; OnPropertyChanged("Image2"); }
        }

        //.........................Значение расстояния для двух выбранных изображений
        public float Distance
        {
            get { return distance; }
            set { distance = value; OnPropertyChanged("Distance"); }
        }

        //.........................Значение сходства для двух выбранных изображений
        public float Similarity
        {
            get { return similarity; }
            set { similarity = value; OnPropertyChanged("Similarity"); }
        }

        //.........................Возможность запуска вычислений (отключается, когда вычисления для двух выбранных изображений уже выполнены)
        public bool ImagesChanged
        {
            get { return changed; }
            set { changed = value; OnPropertyChanged("ImagesChanged"); }
        }

        //.........................Возможность отмены вычислений (отключается, когда нет активных вычислений)
        public bool Cancellable
        {
            get { return cancellable; }
            set { cancellable = value; OnPropertyChanged("Cancellable"); }
        }

        //.........................Коллекция названий изображений из выбранного каталога и их абсолютных путей (для вывода через ListBox)
        public ObservableCollection<ListItem> Files { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        void FilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("Files"); }

        public ViewModel()
        {
            FolderPath = "Folder path will be displayed here";
            Image1 = null;
            Image2 = null;
            
            Files = new ObservableCollection<ListItem>();
            Files.CollectionChanged += FilesCollectionChanged;

            ImagesChanged = true;
            Cancellable = false;
        }
    }

    //________________________________________________________ГЛАВНОЕ ОКНО ПРИЛОЖЕНИЯ_________________________________________________________
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel;
        Functions ArcFace_Functions;
        string DistanceTokenKey;                    // Токен для отмены вычисления расстояния
        string SimilarityTokenKey;                  // Токен для отмены вычисления сходства

        public MainWindow()
        {
            ViewModel = new ViewModel();
            ArcFace_Functions = new Functions();

            InitializeComponent();
            this.DataContext = ViewModel;

            Image1ListBox.SelectionChanged += Image1SelectionChanged;
            Image2ListBox.SelectionChanged += Image2SelectionChanged;
        }

        //.........................Поиск и открытие каталога с изображениями
        private void Browse_Folders_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog open_folder = new FolderBrowserDialog();
            if (open_folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.Image1 = null;
                ViewModel.Image2 = null;

                ViewModel.FolderPath = open_folder.SelectedPath;
                ShowFolderContents();
            }
        }

        //.........................Добавление изображений формата JPG и PNG из выбранного каталога в соответствующие коллекции
        private void ShowFolderContents()
        {
            ViewModel.Files.Clear();

            string[] filepaths = Directory.GetFiles(ViewModel.FolderPath);
            foreach (string filepath in filepaths)
            {
                var listItem = new ListItem(filepath);
                if (listItem.Ext == ".jpg" || listItem.Ext == ".png")
                    ViewModel.Files.Add(listItem);
            }
        }
        private void Image1SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ImagesChanged = true;
            int Index = Image1ListBox.SelectedIndex;
            if(Index != -1)
                ViewModel.Image1 = ViewModel.Files[Index].Path;
            }
        private void Image2SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ImagesChanged = true;
            int Index = Image2ListBox.SelectedIndex;
            if (Index != -1)
                ViewModel.Image2 = ViewModel.Files[Index].Path;
        }

        //.........................Проверка выбора изображений
        private bool CheckSelectedImages()
        {
            if (ViewModel.Files.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select images folder", "Images folder not detected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (ViewModel.Image1 == null && ViewModel.Image2 == null)
            {
                System.Windows.MessageBox.Show("Please select one image from every list", "Images not selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (ViewModel.Image1 == null)
            {
                System.Windows.MessageBox.Show("Please select the first image", "Image not selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (ViewModel.Image2 == null)
            {
                System.Windows.MessageBox.Show("Please select the second image", "Image not selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else return true;
        }

        //.........................Вычисление расстояния и сходства для выбранных изображений
        private async void Start_Calculations_Click(object sender, RoutedEventArgs e)
        {
            Image<Rgb24> face1;
            Image<Rgb24> face2;

            ViewModel.Distance = 0;
            ViewModel.Similarity = 0;
            DistanceTokenKey = Guid.NewGuid().ToString();
            SimilarityTokenKey = Guid.NewGuid().ToString();

            if (ViewModel.ImagesChanged && CheckSelectedImages())
            {
                ViewModel.ImagesChanged = false;
                ViewModel.Cancellable = true;

                face1 = SixLabors.ImageSharp.Image.Load<Rgb24>(ViewModel.Image1);
                face2 = SixLabors.ImageSharp.Image.Load<Rgb24>(ViewModel.Image2);

                var distance = ArcFace_Functions.AsyncDistance(face1, face2, DistanceTokenKey);
                var similarity = ArcFace_Functions.AsyncSimilarity(face1, face2, SimilarityTokenKey);

                pbStatus.Value = 0;

                var ActiveTasks = new List<Task> { distance, similarity };
                while (ActiveTasks.Count > 0)
                {
                    Task finished = await Task.WhenAny(ActiveTasks);
                    if (finished.Status == TaskStatus.Canceled)
                    {
                        ActiveTasks.Clear();
                        ViewModel.Distance = -1;
                        ViewModel.Similarity = -1;
                        ViewModel.ImagesChanged = true;
                        pbStatus.Value = 0;
                        System.Windows.MessageBox.Show("All calculations canceled");
                    }
                    else if (finished == distance)
                    {
                        if (finished.Status == TaskStatus.Canceled);
                        ViewModel.Distance = distance.Result;
                        pbStatus.Value += 50;
                    }
                    else if (finished == similarity)
                    {
                        ViewModel.Similarity = similarity.Result;
                        pbStatus.Value += 50;
                    }
                    else
                        System.Windows.MessageBox.Show("How is this even possible?!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ActiveTasks.Remove(finished);
                }
                ViewModel.Cancellable = false;
            }
        }

        //.........................Отмена вычислений для выбранных изображений
        private async void Cancel_Calculations_Click(object sender, RoutedEventArgs e)
        {
            ArcFace_Functions.Cancel(DistanceTokenKey);
            ArcFace_Functions.Cancel(SimilarityTokenKey);
        }
    }

    //______________________________________________________________КОНВЕРТЕРЫ________________________________________________________________
    public class DistanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                float val = (float)value;
                string distance = val == -1 ? "Canceled" : val.ToString();
                string result = "Distance:  " + distance;
                return result;
            }
            catch
            { return "EX"; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class SimilarityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                float val = (float)value;
                string similarity = val == -1 ? "Canceled" : val.ToString();
                string result = "Similarity: " + similarity;
                return result;
            }
            catch
            { return "EX"; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { throw new NotImplementedException(); }
    }
}
