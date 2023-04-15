using OpenCvSharp;
using System.Text;
using System.Xml.Linq;

namespace Retina
{
    public class UsbWebCam : AbstractVideoSource, IVideoSource
    {
        public int Index;
        public VideoCapture GetCapture()
        {
            return new VideoCapture(Index);
        }

        public void RestoreXml(XElement item)
        {
            throw new System.NotImplementedException();
        }

        public void StoreXml(StringBuilder sb)
        {
            throw new System.NotImplementedException();
        }
    }
}


