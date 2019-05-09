using Rock.Web.Cache;

namespace Rock.Blocks
{
    public abstract class RockBlockType : IRockBlockType
    {
        #region Properties

        /// <summary>
        /// Gets the block identifier.
        /// </summary>
        /// <value>
        /// The block identifier.
        /// </value>
        public int BlockId => BlockCache.Id;

        /// <summary>
        /// Gets or sets the block cache.
        /// </summary>
        /// <value>
        /// The block cache.
        /// </value>
        public BlockCache BlockCache { get; set ; }

        /// <summary>
        /// Gets or sets the page cache.
        /// </summary>
        /// <value>
        /// The page cache.
        /// </value>
        public PageCache PageCache { get; set; }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string GetAttributeValue( string key )
        {
            return BlockCache.GetAttributeValue( key );
        }

        #endregion
    }
}
