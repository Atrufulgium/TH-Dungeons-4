using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.Base {
    public readonly struct Circle {
        public float2 Center { get; init; }
        public float Radius { get; init; }

        public Circle(float2 center, float radius) {
            Center = center;
            Radius = radius;
        }

        public bool IntersectsPoint(float2 point)
            => math.distance(Center, point) < Radius;

        public bool IntersectsCircle(Circle circle)
            => math.distance(Center, circle.Center) < Radius + circle.Radius;
    }
}
