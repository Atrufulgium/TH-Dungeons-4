using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Does one or more gameticks on a single job thread.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct EntireGameTickJob<TGameInput> : IJob where TGameInput : unmanaged, IGameInput {

        BulletField field;
        NativeReference<Player> player;
        NativeList<VM> vms;
        NativeList<BulletReference> createdBullets;

        [NativeDisableUnsafePtrRestriction]
        TGameInput* input;
        NativeReference<Circle> playerHitbox;
        NativeReference<Circle> playerGrazebox;

        NativeList<BulletReference> playerHitboxResult;
        NativeList<BulletReference> playerGrazeboxResult;

        NativeReference<int> gameTick;

        int count;

        public EntireGameTickJob(
            in BulletField field,
            in NativeReference<Player> player,
            in NativeList<VM> vms,
            in NativeList<BulletReference> createdBullets,

            in TGameInput* input,
            in NativeReference<Circle> playerHitbox,
            in NativeReference<Circle> playerGrazebox,

            in NativeList<BulletReference> playerHitboxResult,
            in NativeList<BulletReference> playerGrazeboxResult,

            in NativeReference<int> gameTick,
            in int count
        ) {
            this.field = field;
            this.player = player;
            this.vms = vms;
            this.createdBullets = createdBullets;
            this.input = input;
            this.playerHitbox = playerHitbox;
            this.playerGrazebox = playerGrazebox;
            this.playerHitboxResult = playerHitboxResult;
            this.playerGrazeboxResult = playerGrazeboxResult;
            this.gameTick = gameTick;
            this.count = count;
        }

        public void Execute() {
            for (int i = 0; i < count; i++) {
                ExecuteTick();
            }
        }

        internal unsafe void ExecuteTick() {
            // Run VMS. We only have one thread to work with.
            VMPassMany.Execute(in vms, jobIndex: 0, totalJobs: 1);

            // Parse the outputs of all VMs.
            VMCommandsPass.Execute(in vms, in player, ref field, ref createdBullets);

            // Update positions
            MoveBulletsPass.Execute(
                in field.x,
                in field.y,
                in field.dx,
                in field.dy,
                in field.active
            );
            MovePlayerPass<TGameInput>.Execute(
                in player,
                input,
                ref playerHitbox,
                ref playerGrazebox
            );

            // Check player collisions + graze
            // (Note that bullets may be deleted already)
            BulletCollisionPass.Execute(
                in field.x,
                in field.y,
                in field.radius,
                in field.active,
                in playerHitbox,
                in playerHitboxResult
            );
            BulletCollisionPass.Execute(
                in field.x,
                in field.y,
                in field.radius,
                in field.active,
                in playerGrazebox,
                in playerGrazeboxResult
            );

            // Post-process deletions
            PostProcessPlayerCollisionPass.Execute(
                in player,
                in field,
                in playerHitboxResult,
                in playerGrazeboxResult
            );
            PostProcessDeletionsPass.Execute(ref field);

            // Prepare the next frame
            // TODO: This is the reason IGameInput requires the first field of
            // its implementors be `gameTick`.
            // I haven't found a neater solution.
            IncrementTickPass.Execute(ref gameTick, (int*)input);
        }
    }
}