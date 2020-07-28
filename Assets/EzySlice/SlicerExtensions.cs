using UnityEngine;
using System.Collections;

namespace EzySlice {
    /**
     * Define Extension methods for easy access to slicer functionality
     */
    public static class SlicerExtensions {
        
        /**
         * SlicedHull Return functions and appropriate overrides!
         */

        public static SlicedHull Slice(this GameObject obj, Plane pl, TextureRegion textureRegion, Material crossSectionMaterial = null) {
            return Slicer.Slice(obj, pl, textureRegion, crossSectionMaterial);
        }

        /**
         * These functions (and overrides) will return the final indtaniated GameObjects types
         */
        public static GameObject[] SliceInstantiate(this GameObject obj, Plane pl, bool intersectionOnly) {
            return SliceInstantiate(obj, pl, new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f), intersectionOnly);
        }

        public static GameObject[] SliceInstantiate(this GameObject obj, Plane pl, TextureRegion cuttingRegion, bool intersectionOnly=false, Material crossSectionMaterial = null) {
            SlicedHull slice = Slicer.Slice(obj, pl, cuttingRegion, crossSectionMaterial, intersectionOnly);

            if (slice == null) {
                return null;
            }

            GameObject upperHull = slice.CreateUpperHull(obj, crossSectionMaterial);
            GameObject lowerHull = slice.CreateLowerHull(obj, crossSectionMaterial);

            if (upperHull != null && lowerHull != null) {
                return new GameObject[] { upperHull, lowerHull };
            }

            // otherwise return only the upper hull
            if (upperHull != null) {
                return new GameObject[] { upperHull };
            }

            // otherwise return only the lower hull
            if (lowerHull != null) {
                return new GameObject[] { lowerHull };
            }

            // nothing to return, so return nothing!
            return null;
        }
    }
}
