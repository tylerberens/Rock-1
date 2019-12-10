using System;
using System.Web.Http.Validation;

namespace Rock.Rest.Filters
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Web.Http.Validation.DefaultBodyModelValidator" />
    public class RockRestBodyModelValidator : DefaultBodyModelValidator
    {
        /// <summary>
        /// Determines whether instances of a particular type should be validated.
        /// </summary>
        /// <param name="type">The type to validate.</param>
        /// <returns>
        /// true if the type should be validated; false otherwise.
        /// </returns>
        public override bool ShouldValidateType( Type type )
        {
            // By default, DefaultBodyModelValidator will validate all types, and recursively thru all the types child properties.
            // As it recursively goes thru all the child properties, this will cause our EF models to lazy load all the property values as well!
            // For some objects this could take a really long time (Rock.Model.Group) if there is lots of data in child properties
            bool isIEntity = typeof( Rock.Data.IEntity ).IsAssignableFrom( type );
            bool shouldValidate;
            if ( isIEntity )
            {
                shouldValidate = base.ShouldValidateType( type );
            }
            else
            {
                shouldValidate = false;
            }

            return shouldValidate;
        }
    }
}
