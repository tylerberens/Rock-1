using Rock.Attribute;

namespace Rock.Blocks
{
    /// <summary>
    /// Defines the methods that an ASCX based user control which provides custom
    /// UI for settings must implement.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockCustomSettingsUserControlProvider"/>
    public interface IRockCustomSettingsUserControl
    {
        /// <summary>
        /// Sets the UI values to match the custom settings.
        /// </summary>
        /// <param name="attributeEntity">The attribute entity.</param>
        void SetCustomSettings( IHasAttributes attributeEntity );

        /// <summary>
        /// Gets the custom settings from the UI and updates the entity.
        /// </summary>
        /// <param name="attributeEntity">The attribute entity.</param>
        void GetCustomSettings( IHasAttributes attributeEntity );
    }
}
