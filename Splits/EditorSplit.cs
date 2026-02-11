using UnityEngine;

namespace EditorSpeedSplits.Splits
{
    internal class EditorSplit
    {
        public int index;

        public bool isFinish;

        public float time;
        public float velocity;

        public Vector3 planePosition;
        public Vector3 planeOrientation;

        public Vector3 zeepkistPosition;
        public Vector3 zeepkistOrientation;
        public Vector3 pointOnPlane;

        public Bounds bounds;
    }
}
