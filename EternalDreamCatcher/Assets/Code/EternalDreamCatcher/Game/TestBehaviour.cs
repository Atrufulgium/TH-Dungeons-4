#nullable disable // Monobehaviour start instead of constructor
using Atrufulgium.EternalDreamCatcher.BulletField;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Atrufulgium.EternalDreamCatcher.Game {
    public class TestBehaviour : MonoBehaviour {

        public Field field;
        NativeList<BulletReference> collided;

        private void Awake() {
            field = new();
            collided = new(Allocator.Persistent);
        }

        void Start() {
            var rng = new Unity.Mathematics.Random(230);
            for (int i = 0; i < Field.MAX_BULLETS; i++) {
                var bulletData = new BulletCreationParams(
                    rng.NextFloat2(), rng.NextFloat2Direction() * 0.0005f,
                    0.01f,
                    rng.NextInt(0, 24), 0,
                    (Vector4)Color.HSVToRGB(rng.NextFloat(), 1, 1), new(1,1,1,1),
                    (MiscBulletProps)rng.NextInt(),
                    rng.NextFloat(0.5f, 1.5f) * rng.NextFloat(0.5f, 1.5f) * rng.NextFloat(0.5f, 1.5f)
                );
                field.CreateBullet(ref bulletData );
            }
        }

        void Update() {
            MoveBulletsJob.Run(field);
            for (int ii = 0; ii < 10; ii++)
                BulletCollisionJob.Run(field, new(float2.zero, 0.1f), collided);
        }

        private void OnDestroy() {
            field.Dispose();
            collided.Dispose();
        }
    }
}
