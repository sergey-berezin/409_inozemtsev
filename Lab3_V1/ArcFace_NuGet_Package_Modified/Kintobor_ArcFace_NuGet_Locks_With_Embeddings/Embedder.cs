using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace NuGet_ArcFace_Embedder
{
    public class Embedder
    {
        //...................................PRIVATE PROPERTIES
        private InferenceSession Session;
        private Dictionary<string, Task<float[]>> CalculationsCollection;
        private readonly object locker;

        //...................................PRIVATE METHODS
        private void DownloadNetwork()
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(
                    new System.Uri("https://github.com/onnx/models/raw/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx"),
                    "arcfaceresnet100-8.onnx"
                );
            }
        }

        private float[] Normalize(float[] v) 
        {
            float len = (float)Math.Sqrt(v.Select(x => x*x).Sum());;
            return v.Select(x => x / len).ToArray();
        }

        private DenseTensor<float> ImageToTensor(Image<Rgb24> img)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 3, h, w });

            img.ProcessPixelRows(pa => 
            {
                for (int y=0; y<h; y++)
                {           
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x=0; x<w; x++)
                    {
                        t[0, 0, y, x] = pixelSpan[x].R;
                        t[0, 1, y, x] = pixelSpan[x].G;
                        t[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return t;
        }

        //...................................PUBLIC METHODS
        public Embedder()
        {
            if (!File.Exists("arcfaceresnet100-8.onnx"))
                DownloadNetwork();
            
            this.Session = new InferenceSession("arcfaceresnet100-8.onnx");
            this.CalculationsCollection = new Dictionary<string, Task<float[]>>();
            this.locker = new object();
        }

        ~Embedder()
        { this.Session.Dispose(); }

        public Task<float[]> CreateEmbedding(Image<Rgb24> img)
        {
            Func<float[]> embeddings = 
                () =>
                {
                    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(img)) };
                    IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results;
                    
                    lock(locker)
                    { results = Session.Run(inputs); }
                    
                    return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
                };
            
            var new_task = new Task<float[]>(embeddings, TaskCreationOptions.LongRunning);
            new_task = Task<float[]>.Run(embeddings);
            return new_task;
        }

        public string Embed(Task<float[]> current_task)
        {
            string session_key = Guid.NewGuid().ToString();
            lock(locker)
            { CalculationsCollection[session_key] = current_task; }

            return session_key;
        }

        public float[] GetEmbeddings(string session_key)
        {
            Task<float[]> task;
            lock(locker)
            {
                if (!CalculationsCollection.ContainsKey(session_key))
                    throw new Exception("!!! ERROR: Session key not found !!!\n");

                task = CalculationsCollection[session_key];
                CalculationsCollection.Remove(session_key);
            }
            return task.Result;
        }
    }
}