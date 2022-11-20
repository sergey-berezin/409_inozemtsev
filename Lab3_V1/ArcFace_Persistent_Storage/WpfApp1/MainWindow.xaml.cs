using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Sqlite;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using NuGet_ArcFace_Functions;


namespace WpfApp1
{
    //_____________________________________________________ЭЛЕМЕНТЫ ПОСТОЯННОГО ХРАНИЛИЩА_____________________________________________________
    public class Image
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public byte[] Embedding { get; set; }

        public static string GetHash(string image_path)
        {
            byte[] image_data = File.ReadAllBytes(image_path);
            
            using (var sha256 = SHA256.Create())
            { return string.Concat(sha256.ComputeHash(image_data).Select(x => x.ToString("X2"))); }
        }
    }

    public class DataBaseContext : DbContext
    {
        public DbSet<Image> Images { get; set; }

        public DataBaseContext() { Database.EnsureCreated(); }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("Data Source=ImageEmbeddings.db");
    }

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
        private Image img_from_storage;
        private float distance;
        private float similarity;
        private bool changed;
        private bool cancellable;
        private bool selected;

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

        //.........................Изображение из хранилища, выбранное для удаления
        public Image ImageFromStorage
        {
            get { return img_from_storage; }
            set { img_from_storage = value; OnPropertyChanged("ImageFromStorage"); }
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

        //.........................Подтверждение выбора изображения для удаления из хранилища (иначе нажать кнопку удаления будет невозможно)
        public bool ImageSelected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("ImageSelected"); }
        }

        //.........................Коллекция названий изображений из выбранного каталога и их абсолютных путей (для вывода через ListBox)
        public ObservableCollection<ListItem> Files { get; set; }

        //.........................Коллекция изображений, сохраненных в постоянном хранилище
        public ObservableCollection<Image> ImagesFromStorage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        void FilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("Files"); }
        void ImagesFromStorageCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("ImagesFromStorage"); }

        public ViewModel()
        {
            FolderPath = "Folder path will be displayed here";
            Image1 = null;
            Image2 = null;

            Files = new ObservableCollection<ListItem>();
            ImagesFromStorage = new ObservableCollection<Image>();
            using (var db = new DataBaseContext())
            {
                var query = db.Images;
                foreach (var img in query)
                    ImagesFromStorage.Add(img);
            }

            Files.CollectionChanged += FilesCollectionChanged;
            ImagesFromStorage.CollectionChanged += ImagesFromStorageCollectionChanged;

            ImagesChanged = true;
            Cancellable = false;
            ImageSelected = false;
        }
    }

    //________________________________________________________ГЛАВНОЕ ОКНО ПРИЛОЖЕНИЯ_________________________________________________________
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel;
        Functions ArcFace_Functions;
        private string DistanceTokenKey;                // Токен для отмены вычисления расстояния
        private string SimilarityTokenKey;              // Токен для отмены вычисления сходства
        private SemaphoreSlim Sem;

        public MainWindow()
        {
            ViewModel = new ViewModel();
            ArcFace_Functions = new Functions();
            this.Sem = new SemaphoreSlim(1);

            InitializeComponent();
            this.DataContext = ViewModel;

            Image1ListBox.SelectionChanged += Image1SelectionChanged;
            Image2ListBox.SelectionChanged += Image2SelectionChanged;
            ImagesFromStorageListBox.SelectionChanged += ImageFromStorageSelectionChanged;
        }

        //.........................Индикаторы выбора элементов ListBox
        private void Image1SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ImagesChanged = true;
            int Index = Image1ListBox.SelectedIndex;
            if (Index != -1)
                ViewModel.Image1 = ViewModel.Files[Index].Path;
        }
        private void Image2SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ImagesChanged = true;
            int Index = Image2ListBox.SelectedIndex;
            if (Index != -1)
                ViewModel.Image2 = ViewModel.Files[Index].Path;
        }
        private void ImageFromStorageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int Index = ImagesFromStorageListBox.SelectedIndex;
            if (Index != -1)
            {
                ViewModel.ImageFromStorage = ViewModel.ImagesFromStorage[Index];
                ViewModel.ImageSelected = true;
            }
            else
                ViewModel.ImageSelected = false;
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

        //.........................Приведение выбранного изображения к необходимому размеру перед началом вычислений
        private Image<Rgb24> GetValidSizeImage(Image<Rgb24> source_image)
        {
            Image<Rgb24> valid_size_image = source_image;
            var source_height = source_image.Height;
            var source_width = source_image.Width;

            if (source_height != source_width)
            {
                var target_size = Math.Min(source_height, source_width);
                valid_size_image = source_image.Clone(img =>
                    img.Resize(source_width, source_height).Crop(new Rectangle((source_width - target_size) / 2, 0, target_size, target_size)));
            }
            valid_size_image.Mutate(img => img.Resize(112, 112));
            return valid_size_image;
        }

        //.........................Конвертеры из float[] в byte[] и наоборот (для записи и использования векторов Embedding, сохраненных в хранилище)
        private byte[] FloatToByte(float[] FloatArray)
        {
            byte[] ByteArray = new byte[FloatArray.Length * 4];
            Buffer.BlockCopy(FloatArray, 0, ByteArray, 0, ByteArray.Length);
            return ByteArray;
        }
        private float[] ByteToFloat(byte[] ByteArray)
        {
            float[] FloatArray = new float[ByteArray.Length / 4];
            Buffer.BlockCopy(ByteArray, 0, FloatArray, 0, ByteArray.Length);
            return FloatArray;
        }

        //.........................Получение вектора Embedding для выбранного изображения (либо из хранилища, либо путем анализа изображения)
        private Task<float[]> GetEmbedding(string image_path)
        {
            Task<float[]> embedding_task;
            Image image_from_storage = null;

            // Проверка наличия изображения в хранилище
            using (var db = new DataBaseContext())
            {
                string image_hash = Image.GetHash(image_path);
                var q = db.Images.Where(x => x.Hash == image_hash);
                if (q.Any())
                    image_from_storage = q.First();
            }
            // Если изображение есть в хранилище:
            if (image_from_storage != null)
            {
                Func<float[]> embedding = () => { return ByteToFloat(image_from_storage.Embedding); };
                embedding_task = new Task<float[]>(embedding, TaskCreationOptions.LongRunning);
                embedding_task = Task<float[]>.Run(embedding);
            }
            // Если изображения нет в хранилище:
            else
            {
                var valid_size_face = GetValidSizeImage(SixLabors.ImageSharp.Image.Load<Rgb24>(image_path));
                embedding_task = ArcFace_Functions.CreateEmbedding(valid_size_face);
            }

            return embedding_task;
        }

        private void AddToDataBase(Image img)
        {
            Image image_from_database = null;

            // Проверка наличия изображения в хранилище
            using (var db = new DataBaseContext())
            {
                string new_image_hash = Image.GetHash(img.Path);
                var q = db.Images.Where(x => x.Hash == new_image_hash);
                if (q.Any())
                    image_from_database = q.First();

                // Если изображения нет в хранилище:
                if (image_from_database == null)
                {
                    db.Add(img);
                    db.SaveChanges();

                    ViewModel.ImagesFromStorage.Clear();
                    var query = db.Images;
                    foreach (var a in query)
                        ViewModel.ImagesFromStorage.Add(a);
                }
            }
        }

        //.........................Вычисление расстояния и сходства для выбранных изображений
        private async void Start_Calculations_Click(object sender, RoutedEventArgs e)
        {
            Task<float[]>[] image_embeddings = new Task<float[]>[2];

            ViewModel.Distance = 0;
            ViewModel.Similarity = 0;
            DistanceTokenKey = Guid.NewGuid().ToString();
            SimilarityTokenKey = Guid.NewGuid().ToString();
            pbStatus.Value = 0;

            if (ViewModel.ImagesChanged && CheckSelectedImages())
            {
                ViewModel.ImagesChanged = false;
                ViewModel.Cancellable = true;

                image_embeddings[0] = GetEmbedding(ViewModel.Image1);
                pbStatus.Value += 10;
                image_embeddings[1] = GetEmbedding(ViewModel.Image2);
                pbStatus.Value += 10;

                var distance = ArcFace_Functions.AsyncDistance(image_embeddings[0], image_embeddings[1], DistanceTokenKey);
                var similarity = ArcFace_Functions.AsyncSimilarity(image_embeddings[0], image_embeddings[1], SimilarityTokenKey);

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
                        System.Windows.MessageBox.Show("All calculations canceled!");
                    }
                    else
                    {
                        if (finished == distance)
                        {
                            if (finished.Status == TaskStatus.Canceled) ;
                            ViewModel.Distance = distance.Result;
                            pbStatus.Value += 40;
                        }
                        else if (finished == similarity)
                        {
                            ViewModel.Similarity = similarity.Result;
                            pbStatus.Value += 40;
                        }
                        else
                            System.Windows.MessageBox.Show("How is this even possible?!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        
                        AddToDataBase(
                            new Image
                            {
                                Name = System.IO.Path.GetFileName(ViewModel.Image1),
                                Path = ViewModel.Image1,
                                Hash = Image.GetHash(ViewModel.Image1),
                                Embedding = FloatToByte(image_embeddings[0].Result)
                            });
                        AddToDataBase(
                            new Image
                            {
                                Name = System.IO.Path.GetFileName(ViewModel.Image2),
                                Path = ViewModel.Image2,
                                Hash = Image.GetHash(ViewModel.Image2),
                                Embedding = FloatToByte(image_embeddings[1].Result)
                            });
                    }
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

        //.........................Удаление выбранного изображения из хранилища
        private async void Delete_Image_Click(object sender, RoutedEventArgs e)
        {
            await Sem.WaitAsync();
            using (var db = new DataBaseContext())
            {
                db.Remove(db.Images.Single(img => img.Hash == ViewModel.ImageFromStorage.Hash));
                db.SaveChanges();

                ViewModel.ImagesFromStorage.Clear();
                var query = db.Images;
                foreach (var a in query)
                    ViewModel.ImagesFromStorage.Add(a);
            }
            Sem.Release();
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
