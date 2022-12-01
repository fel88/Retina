using OpenCvSharp;

namespace Retina
{
    public class SearchFaceResult
    {
        public string FilePath;
        public Mat Target;
        public RecognizedFaceInfo[] Faces;
    }
}
