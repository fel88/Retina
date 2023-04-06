using System.Text;
using System.Xml.Linq;

namespace Retina
{
    public interface IVideoSource
    {
        string Name { get; set; }
        string Description { get; }

        void RestoreXml(XElement item);
        void StoreXml(StringBuilder sb);
    }
}


