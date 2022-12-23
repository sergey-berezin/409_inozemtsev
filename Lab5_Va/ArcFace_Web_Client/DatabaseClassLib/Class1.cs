using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace Database
{
    //_____________________________________________________ЭЛЕМЕНТЫ ПОСТОЯННОГО ХРАНИЛИЩА_____________________________________________________
    public class Image
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
        public string Hash { get; set; }
        public byte[] Embedding { get; set; }

        // Создание хэш-кода из абсолютного пути к изображению
        public static string GetHash(string image_path)
        {
            byte[] image_data = File.ReadAllBytes(image_path);

            using (var sha256 = SHA256.Create())
            { return string.Concat(sha256.ComputeHash(image_data).Select(x => x.ToString("X2"))); }
        }
        // Создание хэш-кода из массива байт изображения
        public static string GetHash(byte[] image_data)
        {
            using (var sha256 = SHA256.Create())
            { return string.Concat(sha256.ComputeHash(image_data).Select(x => x.ToString("X2"))); }
        }
    }

    public class Converters
    {
        // Конвертеры из float[] в byte[] и наоборот (для записи и использования векторов Embedding, сохраненных в хранилище)
        public static byte[] FloatToByte(float[] FloatArray)
        {
            byte[] ByteArray = new byte[FloatArray.Length * 4];
            Buffer.BlockCopy(FloatArray, 0, ByteArray, 0, ByteArray.Length);
            return ByteArray;
        }
        public static float[] ByteToFloat(byte[] ByteArray)
        {
            float[] FloatArray = new float[ByteArray.Length / 4];
            Buffer.BlockCopy(ByteArray, 0, FloatArray, 0, ByteArray.Length);
            return FloatArray;
        }
    }

    public class Context : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public Context() { Database.EnsureCreated(); }
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("Data Source=ImageEmbeddings.db");
    }

    public class PostData
    {
        public string Name { get; set; }
        public string Base64String { get; set; }
    }
}