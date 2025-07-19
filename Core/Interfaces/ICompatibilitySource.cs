

namespace PlexiPark.Core.Interfaces
{
    /// <summary>
    /// Anything that can report how compatible
    /// it is with a given visitor type.
    /// </summary>
    public interface ICompatibilitySource<T>
    {
        /// <param name="subject">The thing being evaluated (visitor type or GameObject).</param>
        /// <param name="vType">The visitor’s own type.</param>
        float GetCompatibility(T subject, PlexiPark.Core.SharedEnums.VisitorType vType);
    }
}
