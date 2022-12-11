using Microsoft.EntityFrameworkCore.Design;
using Database;

namespace ServerClasses.DbInterface
{
    public interface IDatabase
    {
        Task<(bool, int)> PostImage(string image_path, CancellationToken token);
        Task<(bool, int[])> GetAllImages(CancellationToken token);
        Task<(bool, Database.Image)> TryGetImageByID(int id, CancellationToken token);
        Task<int> DeleteAllImages(CancellationToken token);
    }

    public class DbFunctions : ServerFunctions, IDatabase
    {
        //.........................POST: Вычисление вектора Embedding изображения и добавление его в хранилище
        public async Task<(bool, int)> PostImage(string image_path, CancellationToken token)
        {
            var ID = await GetEmbedding(image_path);
            if (token.IsCancellationRequested)
                return (false, -1);
            return (true, ID);
        }

        //.........................GET: Получение массива идентификаторов всех изображений в хранилище
        public async Task<(bool, int[]?)> GetAllImages(CancellationToken token)
        {
            var ImageIDs = await GetAllImages();
            if (token.IsCancellationRequested)
                return (false, null);
            return (true, ImageIDs);
        }

        //.........................GET: Получение изображения по его индентификатору в хранилище
        public async Task<(bool, Database.Image?)> TryGetImageByID(int id, CancellationToken token)
        {
            var FoundImage = await GetImageByID(id);
            if (token.IsCancellationRequested)
                return (false, null);
            return (true, FoundImage);
        }

        //.........................DELETE: Удаление всех изображений из хранилища
        public async Task<int> DeleteAllImages(CancellationToken token)
        { return await DeleteImages(token); }
    }
}