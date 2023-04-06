using OpenCvSharp;
using System.Text;
using System.Xml.Linq;

namespace Retina
{
    public class VideoFile : AbstractVideoSource, IVideoSource
    {
        public string Path;
        public override string Description => Path;

        public void RestoreXml(XElement item)
        {
            if (item.Attribute("name") != null)
                Name = item.Attribute("name").Value;

            Path = item.Value;
        }

        public void StoreXml(StringBuilder sb)
        {
            sb.AppendLine($"<file name=\"{Name}\">");
            var d = new XCData(Path);
            sb.AppendLine(d.ToString());
            sb.AppendLine("</file>");
        }
    }
}


