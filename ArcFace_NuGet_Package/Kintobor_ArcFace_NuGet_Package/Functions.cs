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
        private Mutex CancellationTokensMutex;

        //...................................PRIVATE METHODS
        private delegate float CalculationCallback(float[] v1, float[] v2);

        private float Execute(Image<Rgb24> img1, Image<Rgb24> img2, CalculationCallback callback)
        {
            string key1 = embedder.Embed(img1);
            string key2 = embedder.Embed(img2);

            float[] embeddings1 = embedder.GetEmbeddings(key1);
            float[] embeddings2 = embedder.GetEmbeddings(key2);
            return callback(embeddings1, embeddings2);
        }

        private (Task<float> Task, string CancellationTokenKey) ExecuteAsync(Image<Rgb24> img1, Image<Rgb24> img2, CalculationCallback callback)
        {
            string key1 = embedder.Embed(img1);
            string key2 = embedder.Embed(img2);
            string token_key = Guid.NewGuid().ToString();
            CancellationTokenSource token_source = new CancellationTokenSource();

            CancellationTokensMutex.WaitOne();
            CancellationTokensCollection[token_key] = token_source;
            CancellationTokensMutex.ReleaseMutex();

            Func<float> embeddings =
                () =>
                {
                    float[] embeddings1 = embedder.GetEmbeddings(key1);
                    float[] embeddings2 = embedder.GetEmbeddings(key2);
                    return callback(embeddings1, embeddings2);
                };
            return (Task<float>.Run(embeddings, token_source.Token), token_key);
        }
        
        //...................................PUBLIC METHODS
        public Functions()
        {
            this.embedder = new Embedder();
            this.CancellationTokensCollection = new Dictionary<string, CancellationTokenSource>();
            this.CancellationTokensMutex = new Mutex();
        }

        public float Distance(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute(img1, img2, Distance); }

        public float Similarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute(img1, img2, Similarity); }
        
        public (Task<float> task, string CancellationTokenKey) AsyncDistance(Image<Rgb24> img1, Image<Rgb24> img2)
        { return ExecuteAsync(img1, img2, Distance); }

        public (Task<float> task, string CancellationTokenKey) AsyncSimilarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return ExecuteAsync(img1, img2, Similarity); }

        public bool Cancel(string token_key)
        {
            CancellationTokensMutex.WaitOne();

            if (!CancellationTokensCollection.ContainsKey(token_key))
            {
                CancellationTokensMutex.ReleaseMutex();
                return false;
            }
            CancellationTokensCollection[token_key].Cancel();
            CancellationTokensCollection.Remove(token_key);
            CancellationTokensMutex.ReleaseMutex();
            
            return true;
        }
    }
}