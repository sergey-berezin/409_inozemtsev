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

        private T Execute<T>(Image<Rgb24> img1, Image<Rgb24> img2, CalculationCallback<T> callback)
        {
            string key1 = embedder.Embed(img1);
            string key2 = embedder.Embed(img2);

            float[] embeddings1 = embedder.GetEmbeddings(key1);
            float[] embeddings2 = embedder.GetEmbeddings(key2);
            return callback(embeddings1, embeddings2);
        }

        private async Task<float> ExecuteAsync(Image<Rgb24> img1, Image<Rgb24> img2,
                                               CalculationCallback<float> callback,
                                               string cancellation_token_key)
        {
            string key1 = embedder.Embed(img1);
            string key2 = embedder.Embed(img2);
            var cancellation_token_source = new CancellationTokenSource();

            lock(locker)
            { CancellationTokensCollection[cancellation_token_key] = cancellation_token_source; }

            Func<float> embeddings =
                () =>
                {
                    float[] embeddings1 = embedder.GetEmbeddings(key1);
                    float[] embeddings2 = embedder.GetEmbeddings(key2);
                    return callback(embeddings1, embeddings2);
                };
            var res = await Task<float>.Run(embeddings, cancellation_token_source.Token);
            return res;
        }
        
        //...................................PUBLIC METHODS
        public Functions()
        {
            this.embedder = new Embedder();
            this.CancellationTokensCollection = new Dictionary<string, CancellationTokenSource>();
            this.locker = new object();
        }
        
        public float Distance(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute<float>(img1, img2, Distance); }

        public float Similarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute<float>(img1, img2, Similarity); }

        public (float distance, float similarity) Distance_and_Similarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return (Execute<float>(img1, img2, Distance), Execute<float>(img1, img2, Similarity)); }
        
        public async Task<float> AsyncDistance(Image<Rgb24> img1, Image<Rgb24> img2, string cancellation_token_key)
        {
            var res = await ExecuteAsync(img1, img2, Distance, cancellation_token_key);
            return res;
        }

        public async Task<float> AsyncSimilarity(Image<Rgb24> img1, Image<Rgb24> img2, string cancellation_token_key)
        {
            var res = await ExecuteAsync(img1, img2, Similarity, cancellation_token_key);
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