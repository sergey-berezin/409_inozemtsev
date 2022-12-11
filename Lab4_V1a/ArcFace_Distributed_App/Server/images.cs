using System.Collections.ObjectModel;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using NuGet_ArcFace_Functions;

using Database;


namespace ServerClasses
{
    //____________________________________________КЛАСС ФУНКЦИЙ, ВЫПОЛНЯЮЩИХСЯ СО СТОРОНЫ СЕРВЕРА_____________________________________________
    public class ServerFunctions : MarshalByRefObject
    {
        private Functions ArcFace_Functions;        // Класс функций ArcFace для вычислений расстояния и сходства
        private SemaphoreSlim Sem;

        public ServerFunctions()
        {
            this.ArcFace_Functions = new Functions();
            this.Sem = new SemaphoreSlim(1);
        }

        public ObservableCollection<Database.Image> MakeCollection()
        {
            using (var db = new Database.Context())
            { return new ObservableCollection<Database.Image>(db.Images); }
        }

        //.........................Приведение изображения к необходимому размеру перед началом вычислений
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

        //.........................Вычисление вектора Embedding изображения и добавление его в хранилище (если такого изображения там еще нет)
        public async Task<int> GetEmbedding(string image_path)
        {
            try
            {
                Task<float[]> embedding_task;
                Database.Image image_from_storage = null;
                await Sem.WaitAsync();

                // Проверка наличия изображения в хранилище
                using (var db = new Database.Context())
                {
                    string image_hash = Database.Image.GetHash(image_path);
                    var q = db.Images.Where(x => x.Hash == image_hash);
                    if (q.Any())
                        image_from_storage = q.First();
                }
                Sem.Release();

                // Если изображения нет в хранилище:
                if (image_from_storage == null)
                {
                    // Вычисляем вектор Embedding изображения
                    var valid_size_face = GetValidSizeImage(SixLabors.ImageSharp.Image.Load<Rgb24>(image_path));
                    embedding_task = ArcFace_Functions.CreateEmbedding(valid_size_face);
                    await Sem.WaitAsync();

                    // Добавляем изображение в хранилище
                    using (var db = new Database.Context())
                    {
                        string image_hash = Database.Image.GetHash(image_path);
                        db.Add(
                            new Database.Image
                            {
                                Name = System.IO.Path.GetFileName(image_path),
                                Path = image_path,
                                Hash = image_hash,
                                Embedding = Converters.FloatToByte(embedding_task.Result)
                            }
                        );
                        db.SaveChanges();
                        image_from_storage = db.Images.Where(x => x.Hash == image_hash).First();
                    }
                    Sem.Release();
                }
                return image_from_storage.ID;
            }
            catch(Exception ex)
            { return -1; }
        }

        //.........................Получение массива идентификаторов всех изображений в хранилище
        public async Task<int[]> GetAllImages()
        {
            int[] ImageIDs = new int[] {1, 2};

            await Sem.WaitAsync();
            using (var db = new Database.Context())
            {
                var images = new ObservableCollection<Database.Image>(db.Images);
                ImageIDs = new int[images.Count];
                for (int i = 0; i < images.Count; i++)
                    ImageIDs[i] = images[i].ID;
                images.Clear();
            }
            Sem.Release();
            return ImageIDs;
        }

        //.........................Получение изображения по его индентификатору в хранилище
        public async Task<Database.Image> GetImageByID(int id)
        {
            try
            {
                Database.Image found_image;

                await Sem.WaitAsync();
                using (var db = new Database.Context())
                {
                    var q = db.Images.Where(x => x.ID == id);
                    if (q.Any())
                        found_image = q.First();
                    else
                        found_image = null;
                }
                Sem.Release();
                return found_image;
            }
            catch (Exception ex)
            { return null; }
        }

        //.........................Удаление всех изображений из хранилища
        public async Task<int> DeleteImages(CancellationToken token)
        {
            try
            {
                int res = 1;

                await Sem.WaitAsync();
                using (var db = new Database.Context())
                {
                    var buffer = new ObservableCollection<Database.Image>(db.Images);
                    db.Images.RemoveRange(db.Images);
                    db.SaveChanges();

                    if (token.IsCancellationRequested)
                    {
                        for (int i = 0; i < buffer.Count; i++)
                            db.Add(buffer[i]);
                        db.SaveChanges();
                        res = 0;
                    }
                }
                Sem.Release();
                return res;
            }
            catch (Exception ex)
            { return -1; }
        }
    }
}
