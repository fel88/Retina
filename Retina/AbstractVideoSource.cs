using System.Text;
using System.Xml.Linq;

namespace Retina
{
    public abstract class AbstractVideoSource 
    {
        public string Name { get; set; }

        public virtual string Description { get;  }
        
    }
}


