using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using NuGet_ArcFace_Embedder;


namespace NuGet_ArcFace_Functions
{
    public class Functions
    {
        //...................................PRIVATE PROPERTIES
        private float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x*x).Sum());
        private float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
        private float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();
        private Embedder embedder;
        private Dictionary<string, CancellationTokenSource> CancellationTokensCollection;
        private readonly object locker;

        //...................................PRIVATE METHODS
        private delegate T CalculationCallback<T>(float[] v1, float[] v2);

        private T Execute<T>(Task<float[]> embedding1, Task<float[]> embedding2, CalculationCallback<T> callback)
        {
            string key1 = embedder.Embed(embedding1);
            string key2 = embedder.Embed(embedding2);

            float[] embeddings1 = embedder.GetEmbeddings(key1);
            float[] embeddings2 = embedder.GetEmbeddings(key2);
            return callback(embeddings1, embeddings2);
        }

        private async Task<float> ExecuteAsync(Task<float[]> embedding1, Task<float[]> embedding2,
                                               CalculationCallback<float> callback,
                                               string cancellation_token_key)
        {
            string key1 = embedder.Embed(embedding1);
            string key2 = embedder.Embed(embedding2);
            var cancellation_token_source = new CancellationTokenSource();

            lock(locker)
            { CancellationTokensCollection[cancellation_token_key] = cancellation_token_source; }

            var res = await Task<float>.Run(
                () =>
                {
                    float[] embeddings1 = embedder.GetEmbeddings(key1);
                    float[] embeddings2 = embedder.GetEmbeddings(key2);

                    Thread.Sleep(3000);
                    if (cancellation_token_source.Token.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                    return callback(embeddings1, embeddings2);
                }, cancellation_token_source.Token);
            return res;
        }
        
        //...................................PUBLIC METHODS
        public Functions()
        {
            this.embedder = new Embedder();
            this.CancellationTokensCollection = new Dictionary<string, CancellationTokenSource>();
            this.locker = new object();
        }

        public Task<float[]> CreateEmbedding(Image<Rgb24> img) { return embedder.CreateEmbedding(img); }
        
        public float Distance(Task<float[]> embedding1, Task<float[]> embedding2)
        { return Execute<float>(embedding1, embedding2, Distance); }

        public float Similarity(Task<float[]> embedding1, Task<float[]> embedding2)
        { return Execute<float>(embedding1, embedding2, Similarity); }

        public (float distance, float similarity) Distance_and_Similarity(Task<float[]> embedding1, Task<float[]> embedding2)
        { return (Execute<float>(embedding1, embedding2, Distance), Execute<float>(embedding1, embedding2, Similarity)); }
        
        public async Task<float> AsyncDistance(Task<float[]> embedding1, Task<float[]> embedding2, string cancellation_token_key)
        {
            var res = await ExecuteAsync(embedding1, embedding2, Distance, cancellation_token_key);
            return res;
        }

        public async Task<float> AsyncSimilarity(Task<float[]> embedding1, Task<float[]> embedding2, string cancellation_token_key)
        {
            var res = await ExecuteAsync(embedding1, embedding2, Similarity, cancellation_token_key);
            return res;
        }

        public bool Cancel(string token_key)
        {
            bool contains_key = false;
            
            lock(locker)
            {
                if (CancellationTokensCollection.ContainsKey(token_key))
                {
                    CancellationTokensCollection[token_key].Cancel();
                    CancellationTokensCollection.Remove(token_key);
                    contains_key = true;
                }
            }
            return contains_key;
        }
    }
}