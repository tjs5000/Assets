namespace PlexiPark.Core.Interfaces
{
    /// <summary>
    /// Allows sampling the terrain height at an arbitrary world-space XZ.
    /// </summary>
    public interface IHeightSampler
    {
        /// <param name="worldX">X in world space.</param>
        /// <param name="worldZ">Z in world space.</param>
        float SampleHeight(float worldX, float worldZ);
    }
}