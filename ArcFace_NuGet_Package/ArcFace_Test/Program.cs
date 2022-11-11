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

    static string GenreateTokenKey()
    { return Guid.NewGuid().ToString(); }

    //...................................СИНХРОННЫЙ ТЕСТ (ВЫЧИСЛЕНИЕ СХОЖЕСТИ И РАССТОЯНИЯ ПО ОТДЕЛЬНОСТИ)
    static (float[,] distance, float[,] similarity) SeparateSyncTest()
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

    //...................................СИНХРОННЫЙ ТЕСТ (ВЫЧИСЛЕНИЕ СХОЖЕСТИ И РАССТОЯНИЯ ОДНОВРЕМЕННО)
    static(float distance, float similarity)[,] SimultaniousSyncTest()
    {
        (float d, float s)[,] res = new (float, float)[2, 2];
        
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
                res[i, j] = functions.Distance_and_Similarity(faces[i], faces[j]);
        return res;
    }

    //...................................АСИНХРОННЫЙ ТЕСТ
    static async Task<(float[,] distance, float[,] similarity)> AsyncTest()
    {
        float[,] d_res = new float[2, 2];
        float[,] s_res = new float[2, 2];

        var test11 = functions.AsyncDistance(faces[0], faces[0], GenreateTokenKey());
        var test12 = functions.AsyncDistance(faces[0], faces[1], GenreateTokenKey());
        var test21 = functions.AsyncDistance(faces[1], faces[0], GenreateTokenKey());
        var test22 = functions.AsyncDistance(faces[1], faces[1], GenreateTokenKey());

        var ActiveTests = new List<Task> {test11, test12, test21, test22};
        while (ActiveTests.Count > 0)
        {
            Task finished = await Task.WhenAny(ActiveTests);
            if (finished == test11)
            {
                d_res[0, 0] = test11.Result;
                Console.WriteLine("Distance Test[1,1] finished!");
            }
            else if (finished == test12)
            {
                d_res[0, 1] = test12.Result;
                Console.WriteLine("Distance Test[1,2] finished!");
            }
            else if (finished == test21)
            {
                d_res[1, 0] = test21.Result;
                Console.WriteLine("Distance Test[2,1] finished!");
            }
            else if (finished == test22)
            {
                d_res[1, 1] = test22.Result;
                Console.WriteLine("Distance Test[2,2] finished!");
            }
            else
                Console.WriteLine("ERROR!");
            ActiveTests.Remove(finished);
        }
        
        test11 = functions.AsyncSimilarity(faces[0], faces[0], GenreateTokenKey());
        test12 = functions.AsyncSimilarity(faces[0], faces[1], GenreateTokenKey());
        test21 = functions.AsyncSimilarity(faces[1], faces[0], GenreateTokenKey());
        test22 = functions.AsyncSimilarity(faces[1], faces[1], GenreateTokenKey());

        ActiveTests = new List<Task> {test11, test12, test21, test22};
        while (ActiveTests.Count > 0)
        {
            Task finished = await Task.WhenAny(ActiveTests);
            if (finished == test11)
            {
                s_res[0, 0] = test11.Result;
                Console.WriteLine("Similarity Test[1,1] finished!");
            }
            else if (finished == test12)
            {
                s_res[0, 1] = test12.Result;
                Console.WriteLine("Similarity Test[1,2] finished!");
            }
            else if (finished == test21)
            {
                s_res[1, 0] = test21.Result;
                Console.WriteLine("Similarity Test[2,1] finished!");
            }
            else if (finished == test22)
            {
                s_res[1, 1] = test22.Result;
                Console.WriteLine("Similarity Test[2,2] finished!");
            }
            else
                Console.WriteLine("ERROR!");
            ActiveTests.Remove(finished);
        }

        return (d_res, s_res);
    }

    //...................................АСИНХРОННЫЙ ТЕСТ С ОТМЕНОЙ ВЫЧИСЛЕНИЯ РАССТОЯНИЯ И СХОЖЕСТИ МЕЖДУ ОДНИМ И ТО ЖЕ ИЗОБРАЖЕНИЕМ
    static async Task<(float[,] distance, float[,] similarity)> AsyncCancelTest()
    {
        float[,] d_res = new float[2, 2]{{-1, -1}, {-1, -1}};
        float[,] s_res = new float[2, 2]{{-1, -1}, {-1, -1}};
        string same_faces_test_token1 = GenreateTokenKey();
        string same_faces_test_token2 = GenreateTokenKey();

        var test11 = functions.AsyncDistance(faces[0], faces[0], same_faces_test_token1);
        var test12 = functions.AsyncDistance(faces[0], faces[1], GenreateTokenKey());
        var test21 = functions.AsyncDistance(faces[1], faces[0], GenreateTokenKey());
        var test22 = functions.AsyncDistance(faces[1], faces[1], same_faces_test_token2);
            
        functions.Cancel(same_faces_test_token1);
        functions.Cancel(same_faces_test_token2);
        
        var ActiveTests = new List<Task> {test11, test12, test21, test22};
        while (ActiveTests.Count > 0)
        {
            try
            {
                Task finished = await Task.WhenAny(ActiveTests);
                if (finished == test11)
                {
                    d_res[0, 0] = test11.Result;
                    Console.WriteLine("Distance Test[1,1] finished!");
                }
                else if (finished == test12)
                {
                    d_res[0, 1] = test12.Result;
                    Console.WriteLine("Distance Test[1,2] finished!");
                }
                else if (finished == test21)
                {
                    d_res[1, 0] = test21.Result;
                    Console.WriteLine("Distance Test[2,1] finished!");
                }
                else if (finished == test22)
                {
                    d_res[1, 1] = test22.Result;
                    Console.WriteLine("Distance Test[2,2] finished!");
                }
                else
                    Console.WriteLine("ERROR!");
                ActiveTests.Remove(finished);
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        TaskCanceledException ex = (TaskCanceledException)e;
                        Console.WriteLine("Task was canceled\n");
                        ActiveTests.Remove(ActiveTests.Find(x => x.Id.Equals(ex.Task.Id)));
                    }  
                    else
                        Console.WriteLine(e.Message);
                }
            }
        }
            
        test11 = functions.AsyncSimilarity(faces[0], faces[0], same_faces_test_token1);
        test12 = functions.AsyncSimilarity(faces[0], faces[1], GenreateTokenKey());
        test21 = functions.AsyncSimilarity(faces[1], faces[0], GenreateTokenKey());
        test22 = functions.AsyncSimilarity(faces[1], faces[1], same_faces_test_token2);

        if (functions.Cancel(same_faces_test_token1))
                Console.WriteLine("Distance Test[1,1] successfully cancelled!");
        if (functions.Cancel(same_faces_test_token2))
                Console.WriteLine("Distance Test[2,2] successfully cancelled!");

        ActiveTests = new List<Task> {test11, test12, test21, test22};
        while (ActiveTests.Count > 0)
        {
            try
            {
                Task finished = await Task.WhenAny(ActiveTests);
                if (finished == test11)
                {
                    s_res[0, 0] = test11.Result;
                    Console.WriteLine("Similarity Test[1,1] finished!");
                }
                else if (finished == test12)
                {
                    s_res[0, 1] = test12.Result;
                    Console.WriteLine("Similarity Test[1,2] finished!");
                }
                else if (finished == test21)
                {
                    s_res[1, 0] = test21.Result;
                    Console.WriteLine("Similarity Test[2,1] finished!");
                }
                else if (finished == test22)
                {
                    s_res[1, 1] = test22.Result;
                    Console.WriteLine("Similarity Test[2,2] finished!");
                }
                else
                    Console.WriteLine("ERROR!");
                ActiveTests.Remove(finished);
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        TaskCanceledException ex = (TaskCanceledException)e;
                        Console.WriteLine("Task was canceled\n");
                        ActiveTests.Remove(ActiveTests.Find(x => x.Id.Equals(ex.Task.Id)));
                    }  
                    else
                        Console.WriteLine(e.Message);
                }
            }
        }
        return (d_res, s_res);
    }

    //...................................ПЕЧАТЬ РЕЗУЛЬТАТОВ ТЕСТА
    static void PrintResults((float[,] d, float[,] s) res)
    {
        Console.WriteLine("Distance:");
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
            {
                Console.Write($"    face{i + 1} - face{j + 1} = ");
                Console.WriteLine(res.d[i, j] == -1 ? "Cancelled" : res.d[i, j]);
            }
        Console.WriteLine("\nSimilarity:");
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
            {
                Console.Write($"    face{i + 1} - face{j + 1} = ");
                Console.WriteLine(res.s[i, j] == -1 ? "Cancelled" : res.s[i, j]);
            }
    }

    //...................................ПЕЧАТЬ РЕЗУЛЬТАТОВ ТЕСТА (ВЫЧИСЛЕНИЕ СХОЖЕСТИ И РАССТОЯНИЯ ОДНОВРЕМЕННО)
    static void PrintSimultaniousResults((float d, float s)[,] res)
    {
        for (int i=0; i<2; i++)
            for (int j=0; j<2; j++)
                Console.WriteLine($"face{i + 1} - face{j + 1}:\n" + 
                                  $"    Distance = {res[i, j].d}\n" +
                                  $"    Similarity = {res[i, j].s}\n");
    }

    static async Task Main()
    {
        LoadFaces("C:\\Users\\User\\Desktop\\prog\\C#\\Sem7\\ArcFace_NuGet_Package\\ArcFace_Test\\face1.png", 
                  "C:\\Users\\User\\Desktop\\prog\\C#\\Sem7\\ArcFace_NuGet_Package\\ArcFace_Test\\face2.png");

        Console.WriteLine("\n____________________SYNCHRONOUS SEPARATE OPERATIONS TEST START____________________\n");
        var sep_sync_results = SeparateSyncTest();
        Console.WriteLine("DONE\n");
        PrintResults(sep_sync_results);

        Console.WriteLine("\n__________________SYNCHRONOUS SIMULTANIOUS OPERATIONS TEST START__________________\n");
        var sim_sync_results = SimultaniousSyncTest();
        Console.WriteLine("DONE\n");
        PrintSimultaniousResults(sim_sync_results);

        Console.WriteLine("_____________________________ASYNCHRONOUS TEST START______________________________\n");
        var async_results = await AsyncTest();
        Console.WriteLine("DONE\n");
        PrintResults(async_results);
    
        Console.WriteLine("\n_______________________ASYNCHRONOUS CANCELLATION TEST START_______________________\n");
        var async_cancellation_results = await AsyncCancelTest();
        Console.Write("DONE\n\n");
        PrintResults(async_cancellation_results);
        
        Console.WriteLine("\n________________________________ALL TESTS COMPLETE________________________________\n");

    }
}