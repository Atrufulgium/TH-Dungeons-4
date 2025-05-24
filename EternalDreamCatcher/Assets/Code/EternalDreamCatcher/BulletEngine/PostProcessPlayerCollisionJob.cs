using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Deletes collided bullets and adds to the graze counter. This also
    /// clears the lists for future use.
    /// <br/>
    /// This assumes bullets may have been deleted already.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct PostProcessPlayerCollisionJob : IJob {

        public NativeReference<Player> player;
        public BulletField field;
        [ReadOnly]
        public NativeList<BulletReference> hitList;
        [ReadOnly]
        public NativeList<BulletReference> grazeList;

        public PostProcessPlayerCollisionJob(
            in NativeReference<Player> player,
            in BulletField field,
            in NativeList<BulletReference> hitList,
            in NativeList<BulletReference> grazeList
        ) {
            this.player = player;
            this.field = field;
            this.hitList = hitList;
            this.grazeList = grazeList;
        }

        public readonly unsafe void Execute() {
            PostProcessPlayerCollisionPass.Execute(
                in player, in field, in hitList, in grazeList)
            ;
        }
    }

    /// <inheritdoc cref="PostProcessPlayerCollisionJob"/>
    internal static class PostProcessPlayerCollisionPass {
        public static unsafe void Execute(
            in NativeReference<Player> player,
            in BulletField field,
            in NativeList<BulletReference> hitList,
            in NativeList<BulletReference> grazeList
        ) {
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
