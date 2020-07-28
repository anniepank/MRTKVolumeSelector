using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice {
    /**
     * TextureRegion defines a region of a specific texture which can be used
     * for custom UV Mapping Routines.
     * 
     * TextureRegions are always stored in normalized UV Coordinate space between
     * 0.0f and 1.0f
     */
    public struct TextureRegion {
        private readonly float pos_start_x;
        private readonly float pos_start_y;
        private readonly float pos_end_x;
        private readonly float pos_end_y;

        public TextureRegion(float startX, float startY, float endX, float endY) {
            this.pos_start_x = startX;
            this.pos_start_y = startY;
            this.pos_end_x = endX;
            this.pos_end_y = endY;
        }

        public float startX { get { return this.pos_start_x; } }
        public float startY { get { return this.pos_start_y; } }
        public float endX { get { return this.pos_end_x; } }
        public float endY { get { return this.pos_end_y; } }

        public Vector2 start { get { return new Vector2(startX, startY); } }
        public Vector2 end { get { return new Vector2(endX, endY); } }

        /**
         * Perform a mapping of a UV coordinate (computed in 0,1 space)
         * into the new coordinates defined by the provided TextureRegion
         */
        public Vector2 Map(Vector2 uv) {
            return Map(uv.x, uv.y);
        }

        /**
         * Perform a mapping of a UV coordinate (computed in 0,1 space)
         * into the new coordinates defined by the provided TextureRegion
         */
        public Vector2 Map(float x, float y) {
            float mappedX = MAP(x, 0.0f, 1.0f, pos_start_x, pos_end_x);
            float mappedY = MAP(y, 0.0f, 1.0f, pos_start_y, pos_end_y);

            return new Vector2(mappedX, mappedY);
        }

        /**
         * Our mapping function to map arbitrary values into our required texture region
         */
        private static float MAP(float x, float in_min, float in_max, float out_min, float out_max) {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}
