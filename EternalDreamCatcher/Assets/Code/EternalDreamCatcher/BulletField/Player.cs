using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    public struct Player {
        /// <summary>
        /// Where the player is on the field. This is not bounds-checked in
        /// this struct.
        /// </summary>
        public float2 position;

        /// <summary>
        /// How many lives the player has remaining.
        /// </summary>
        public int remainingLives;
        /// <summary>
        /// How many bombs the player has remaining.
        /// </summary>
        public int RemainingBombs;

        /// <summary>
        /// How fast the player is when focusing, in units/tick.
        /// </summary>
        public float focusedSpeed;
        /// <summary>
        /// How fast the player is when not focusing, in units/tick.
        /// </summary>
        public float unfocusedSpeed;

        /// <summary>
        /// How many units the player hitbox is large.
        /// </summary>
        public float hitboxRadius;
        /// <summary>
        /// How many units the player's grazebox is large.
        /// </summary>
        public float grazeboxRadius;

        /// <summary>
        /// How many bullets the player has grazed.
        /// </summary>
        public int graze;
    }

    /// <summary>
    /// Moves the player depending on the game input.
    /// Also writes the new hit- and grazeboxes.
    /// </summary>
    public struct MovePlayerJob<TGameInput> : IJob where TGameInput : unmanaged, IGameInput {

        public NativeReference<Player> player;
        public NativeReference<TGameInput> gameInput;
        [WriteOnly]
        public NativeReference<Circle> hitbox;
        [WriteOnly]
        public NativeReference<Circle> grazebox;

        public MovePlayerJob(
            in NativeReference<Player> player,
            in NativeReference<TGameInput> gameInput,
            in NativeReference<Circle> hitbox,
            in NativeReference<Circle> grazebox
        ) {
            this.player = player;
            this.gameInput = gameInput;
            this.hitbox = hitbox;
            this.grazebox = grazebox;
        }

        public unsafe void Execute() {
            var p = player.GetUnsafeTypedPtr();
            var g = gameInput.GetUnsafeTypedPtr();
            float2 delta = g->MoveDirection;
            delta = math.normalizesafe(delta);
            delta *= g->IsFocusing ? p->focusedSpeed : p->unfocusedSpeed;
            float2 newPos = p->position + delta;
            newPos = math.clamp(newPos, new(0.02f, 0.05f), new(0.98f, 1.15f));
            p->position = newPos;
            hitbox.Value = new(newPos, p->hitboxRadius);
            grazebox.Value = new(newPos, p->grazeboxRadius);
        }
    }

    /// <summary>
    /// Deletes collided bullets and adds to the graze counter. This also
    /// clears the lists for future use.
    /// <br/>
    /// This assumes bullets may have been deleted already.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct PostProcessPlayerCollisionJob : IJob {

        public NativeReference<Player> player;
        public Field field;
        public NativeList<BulletReference> hitList;
        public NativeList<BulletReference> grazeList;

        public PostProcessPlayerCollisionJob(
            NativeReference<Player> player,
            in Field field,
            NativeList<BulletReference> hitList,
            NativeList<BulletReference> grazeList
        ) {
            this.player = player;
            this.field = field;
            this.hitList = hitList;
            this.grazeList = grazeList;
        }

        public unsafe void Execute() {
            var p = player.GetUnsafeTypedPtr();
            p->graze += grazeList.Length;

            bool hit = false;
            for (int i = 0; i < hitList.Length; i++) {
                if (field.ReferenceIsDeleted(hitList[i]))
                    continue;
                hit = true;
                field.DeleteBullet(hitList[i]);
            }

            if (hit)
                p->remainingLives--;

            hitList.Clear();
            grazeList.Clear();
        }
    }
}
