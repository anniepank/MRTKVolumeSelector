using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice
{

    /**
     * Quick Internal structure which checks where the point lays on the
     * Plane. UP = Upwards from the Normal, DOWN = Downwards from the Normal
     * ON = Point lays straight on the plane
     */
    public enum SideOfPlane
    {
        UP,
        DOWN,
        ON
    }

    /**
     * Represents a simple 3D Plane structure with a position
     * and direction which extends infinitely in its axis. This provides
     * an optimal structure for collision tests for the slicing framework.
     */
    public struct Plane
    {
        private Vector3 m_normal;
        private float m_dist;
        public Vector3 pos;

        // this is for editor debugging only! do NOT try to access this
        // variable at runtime, we will be stripping it out for final
        // builds
#if UNITY_EDITOR
        private Transform trans_ref;
#endif

        public Plane(Vector3 pos, Vector3 norm)
        {
            this.m_normal = norm;
            this.m_dist = Vector3.Dot(norm, pos);
            this.pos = pos;

            // this is for editor debugging only!
#if UNITY_EDITOR
            trans_ref = null;
#endif
        }

        public Vector3 normal
        {
            get { return this.m_normal; }
        }

        public float dist
        {
            get { return this.m_dist; }
        }

        /**
         * Checks which side of the plane the point lays on.
         */
        public SideOfPlane SideOf(Vector3 pt)
        {
            float result = Vector3.Dot(m_normal, pt) - m_dist;

            if (result > Intersector.Epsilon)
            {
                return SideOfPlane.UP;
            }

            if (result < -Intersector.Epsilon)
            {
                return SideOfPlane.DOWN;
            }

            return SideOfPlane.ON;
        }
    }
}