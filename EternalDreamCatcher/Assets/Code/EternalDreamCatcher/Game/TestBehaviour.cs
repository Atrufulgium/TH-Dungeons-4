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
                field.CreateBullet(
                    new(rng.NextFloat2(), rng.NextFloat2Direction() * 0.00005f, 0.01f)
                );
            }
        }

        void Update() {
            for (int i = 0; i < 10; i++) {
                MoveBulletsJob.Run(field);
                for (int ii = 0; ii < 10; ii++)
                    BulletCollisionJob.Run(field, new(float2.zero, 0.1f), collided);
            }
        }

        private void OnDestroy() {
            field.Dispose();
            collided.Dispose();
        }
    }
}
