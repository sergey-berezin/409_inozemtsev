using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace NuGet_ArcFace_Embedder
{
    public class Embedder
    {
        //...................................PRIVATE PROPERTIES
        private InferenceSession Session;
        private Dictionary<string, Task<float[]>> CalculationsCollection;
        private SemaphoreSlim CalcSemaphore;

        //...................................PRIVATE METHODS
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
            this.Session = new InferenceSession("arcfaceresnet100-8.onnx");
            this.CalculationsCollection = new Dictionary<string, Task<float[]>>();
            this.CalcSemaphore = new SemaphoreSlim(1);
        }

        ~Embedder()
        { this.Session.Dispose(); }

        public string Embed(Image<Rgb24> img)
        {
            string session_key = Guid.NewGuid().ToString();
            Func<float[]> embeddings = 
                () =>
                {
                    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(img)) };
                    using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);
                    return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
                };
            Task<float[]> new_task = Task<float[]>.Run(embeddings);

            CalcSemaphore.WaitAsync();
            CalculationsCollection[session_key] = new_task;
            CalcSemaphore.Release();

            return session_key;
        }

        public float[] GetEmbeddings(string session_key)
        {
            CalcSemaphore.WaitAsync();

            if (!CalculationsCollection.ContainsKey(session_key))
                throw new Exception("!!! ERROR: Session key not found !!!\n");

            Task<float[]> task = CalculationsCollection[session_key];
            CalculationsCollection.Remove(session_key);

            CalcSemaphore.Release();
            return task.Result;
        }
    }
}