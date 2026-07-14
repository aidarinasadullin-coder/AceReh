namespace SnowMeltingCalculator.Core.Extensions
{
    /// <summary>
    /// Helpers for converting between length units used across the application.
    /// </summary>
    public static class UnitsConversionExtensions
    {
        /// <summary>
        /// Convert millimeters to centimeters.
        /// </summary>
        /// <param name="mm">Value in millimeters.</param>
        /// <returns>Value in centimeters.</returns>
        public static double MmToCm(this double mm) => mm / 10.0;

        /// <summary>
        /// Convert millimeters to centimeters.
        /// </summary>
        /// <param name="mm">Value in millimeters.</param>
        /// <returns>Value in centimeters.</returns>
        public static double MmToCm(this int mm) => mm / 10.0;

        /// <summary>
        /// Convert centimeters to millimeters.
        /// </summary>
        /// <param name="cm">Value in centimeters.</param>
        /// <returns>Value in millimeters.</returns>
        public static double CmToMm(this double cm) => cm * 10.0;

        /// <summary>
        /// Convert centimeters to millimeters.
        /// </summary>
        /// <param name="cm">Value in centimeters.</param>
        /// <returns>Value in millimeters.</returns>
        public static int CmToMm(this int cm) => cm * 10;
    }
}
