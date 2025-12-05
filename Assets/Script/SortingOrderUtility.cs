using UnityEngine;

namespace Script
{
    /// <summary>
    /// Utility class for calculating sorting order based on Z position
    /// Higher Z values (closer to camera) get higher sorting orders
    /// </summary>
    public static class SortingOrderUtility
    {
        /// <summary>
        /// Default base sorting order
        /// </summary>
        public const int DefaultBaseSortingOrder = 0;
        
        /// <summary>
        /// Default multiplier for Z position to sorting order conversion
        /// </summary>
        public const float DefaultSortingOrderMultiplier = 100f;
        
        /// <summary>
        /// Calculates sorting order based on Z position
        /// Higher Z values (closer to camera) result in higher sorting orders
        /// </summary>
        /// <param name="zPosition">Z position in world space</param>
        /// <param name="baseSortingOrder">Base sorting order (default: 0)</param>
        /// <param name="multiplier">Multiplier for Z position conversion (default: 100)</param>
        /// <returns>Calculated sorting order</returns>
        public static int GetSortingOrderFromZ(float zPosition, int baseSortingOrder = DefaultBaseSortingOrder, float multiplier = DefaultSortingOrderMultiplier)
        {
            // Multiply by -multiplier so that higher Z = higher sorting order (closer to camera)
            return baseSortingOrder + Mathf.RoundToInt(-zPosition * multiplier);
        }
        
        /// <summary>
        /// Calculates sorting order based on world position (uses Z component)
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="baseSortingOrder">Base sorting order (default: 0)</param>
        /// <param name="multiplier">Multiplier for Z position conversion (default: 100)</param>
        /// <returns>Calculated sorting order</returns>
        public static int GetSortingOrderFromPosition(Vector3 worldPosition, int baseSortingOrder = DefaultBaseSortingOrder, float multiplier = DefaultSortingOrderMultiplier)
        {
            return GetSortingOrderFromZ(worldPosition.z, baseSortingOrder, multiplier);
        }
    }
}

