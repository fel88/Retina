using OpenCvSharp;
using System.Text;
using System.Xml.Linq;

namespace Retina
{
    public class RtspCamera : AbstractVideoSource, IVideoSource
    {
        public string Source;
        public override string Description => Source;

        public VideoCapture GetCapture()
        {
            return new VideoCapture(Source);
        }

        public void RestoreXml(XElement item)
        {
            if (item.Attribute("name") != null)
                Name = item.Attribute("name").Value;

            Source = item.Value;
        }

        public void StoreXml(StringBuilder sb)
        {
            sb.AppendLine($"<rtsp name=\"{Name}\">");
            var d = new XCData(Source);
            sb.AppendLine(d.ToString());
            sb.AppendLine("</rtsp>");
        }
    }
}


