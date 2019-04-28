using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Rock.Mobile.Core.Blocks
{
    public abstract class MobileBlockBase : ApiController
    {
        public string GetAttributeValue(string key)
        {
            // TODO: Figure out the implementation

            /*if (BlockCache != null)
            {
                return BlockCache.GetAttributeValue(key);
            }*/

            return null;
        }
    }
}
