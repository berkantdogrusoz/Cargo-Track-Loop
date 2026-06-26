using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    public sealed class LoopPath
    {
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> segmentLengths = new List<float>();
        private float totalLength;

        public float TotalLength
        {
            get { return totalLength; }
        }

        public IReadOnlyList<Vector3> Points
        {
            get { return points; }
        }

        public void SetRoundedRectangle(float width, float depth, float radius, int cornerSteps, float y)
        {
            points.Clear();
            segmentLengths.Clear();
            totalLength = 0f;

            float halfWidth = width * 0.5f - radius;
            float halfDepth = depth * 0.5f - radius;
            AddCorner(new Vector3(halfWidth, y, halfDepth), radius, 0f, 90f, cornerSteps);
            AddCorner(new Vector3(-halfWidth, y, halfDepth), radius, 90f, 180f, cornerSteps);
            AddCorner(new Vector3(-halfWidth, y, -halfDepth), radius, 180f, 270f, cornerSteps);
            AddCorner(new Vector3(halfWidth, y, -halfDepth), radius, 270f, 360f, cornerSteps);
            RecalculateLengths();
        }

        public void SetPoints(IEnumerable<Vector3> sourcePoints)
        {
            points.Clear();
            segmentLengths.Clear();
            totalLength = 0f;
            points.AddRange(sourcePoints);
            RecalculateLengths();
        }

        public Vector3 GetPosition(float distance)
        {
            if (points.Count == 0 || totalLength <= 0f)
            {
                return Vector3.zero;
            }

            distance = Mathf.Repeat(distance, totalLength);
            float traversed = 0f;
            for (int i = 0; i < segmentLengths.Count; i++)
            {
                float segmentLength = segmentLengths[i];
                if (distance <= traversed + segmentLength)
                {
                    float t = segmentLength <= 0.0001f ? 0f : (distance - traversed) / segmentLength;
                    return Vector3.Lerp(points[i], points[(i + 1) % points.Count], t);
                }

                traversed += segmentLength;
            }

            return points[points.Count - 1];
        }

        public Vector3 GetForward(float distance)
        {
            Vector3 a = GetPosition(distance);
            Vector3 b = GetPosition(distance + 0.05f);
            Vector3 direction = b - a;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        public bool DidCross(float previousDistance, float currentDistance, float checkpointDistance)
        {
            previousDistance = Mathf.Repeat(previousDistance, totalLength);
            currentDistance = Mathf.Repeat(currentDistance, totalLength);
            checkpointDistance = Mathf.Repeat(checkpointDistance, totalLength);

            if (currentDistance >= previousDistance)
            {
                return checkpointDistance >= previousDistance && checkpointDistance <= currentDistance;
            }

            return checkpointDistance >= previousDistance || checkpointDistance <= currentDistance;
        }

        private void AddCorner(Vector3 center, float radius, float startDegrees, float endDegrees, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                points.Add(center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void RecalculateLengths()
        {
            for (int i = 0; i < points.Count; i++)
            {
                float length = Vector3.Distance(points[i], points[(i + 1) % points.Count]);
                segmentLengths.Add(length);
                totalLength += length;
            }
        }
    }
}
