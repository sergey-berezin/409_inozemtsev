using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using NuGet_ArcFace_Functions;


class Test
{
    static List<Image<Rgb24>> faces = new List<Image<Rgb24>>(2);
    static Functions functions = new Functions();

    //...................................ЗАГРУЗКА ИЗОБРАЖЕНИЙ
    static void LoadFaces(string face1, string face2)
    {
        faces.Clear();
        faces.Add(Image.Load<Rgb24>(face1));
        faces.Add(Image.Load<Rgb24>(face2));
    }

    //...................................СИНХРОННЫЙ ТЕСТ
    static (float[,] distance, float[,] similarity) SyncTest()
    {
        float[,] d_res = new float[2, 2];
        float[,] s_res = new float[2, 2];
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
            {
                d_res[i, j] = functions.Distance(faces[i], faces[j]);
                s_res[i, j] = functions.Similarity(faces[i], faces[j]);
            }
        return (d_res, s_res);
    }

    //...................................АСИНХРОННЫЙ ТЕСТ
    static (float[,] distance, float[,] similarity) AsyncTest()
    {
        var d_task = new Task<float>[2, 2];
        var s_task = new Task<float>[2, 2];
        float[,] d_res = new float[2, 2];
        float[,] s_res = new float[2, 2];

        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
            {
                d_task[i, j] = functions.AsyncDistance(faces[i], faces[j]).task;
                s_task[i, j] = functions.AsyncSimilarity(faces[i], faces[j]).task;
            
                d_res[i, j] = d_task[i, j].Result;
                s_res[i, j] = s_task[i, j].Result;
            }
        return (d_res, s_res);
    }

    //...................................АСИНХРОННЫЙ ТЕСТ С ОТМЕНОЙ ВЫЧИСЛЕНИЯ РАССТОЯНИЯ
    static (float[,] similarity, bool distance_cancellation_successfull) AsyncCancelTest()
    {
        var d_task = new Task<float>[2, 2];
        var s_task = new Task<float>[2, 2];
        float[,] s_res = new float[2, 2];
        bool calculations_cancelled = false;

        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
            {
                var d_values = functions.AsyncDistance(faces[i], faces[j]);
                d_task[i, j] = d_values.task;
                if (!calculations_cancelled)
                {
                    functions.Cancel(d_values.CancellationTokenKey);
                    calculations_cancelled = true;
                }

                var s_values = functions.AsyncSimilarity(faces[i], faces[j]);
                s_task[i, j] = s_values.task;
                s_res[i, j] = s_task[i, j].Result;
            }
        return (s_res, calculations_cancelled);
    }

    //...................................ПЕЧАТЬ РЕЗУЛЬТАТОВ ТЕСТА
    static void PrintResults(float[,] res)
    {
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
                Console.WriteLine($"face{i + 1} - face{j + 1} = {res[i, j]}");
    }

    static void Main()
    {
        LoadFaces("face1.png", "face2.png");

        Console.WriteLine("\n______________________________SYNCHRONOUS TEST START______________________________\n");
        var sync_results = SyncTest();
        Console.WriteLine("DONE\n\nDistance:");
        PrintResults(sync_results.distance);
        Console.WriteLine("\nSimilarity:");
        PrintResults(sync_results.similarity);

        Console.WriteLine("\n_____________________________ASYNCHRONOUS TEST START______________________________\n");
        var async_results = AsyncTest();
        Console.WriteLine("DONE\n\nDistance:");
        PrintResults(async_results.distance);
        Console.WriteLine("\nSimilarity:");
        PrintResults(async_results.similarity);
    
        Console.WriteLine("\n_______________________ASYNCHRONOUS CANCELLATION TEST START_______________________\n");
        var async_cancellation_results = AsyncCancelTest();
        Console.Write("DONE\n\nDistance cancellation: ");
        Console.WriteLine(async_cancellation_results.distance_cancellation_successfull ? "Successful!\n" : "Unsuccessful!\n");
        Console.WriteLine("Similarity:"); 
        PrintResults(async_cancellation_results.similarity);
        
        Console.WriteLine("\n________________________________ALL TESTS COMPLETE________________________________\n");

    }
}