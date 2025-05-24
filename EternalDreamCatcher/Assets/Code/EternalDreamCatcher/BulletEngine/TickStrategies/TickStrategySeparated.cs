using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies {
    /// <summary>
    /// A strategy that schedules each part of the pipeline separately.
    /// <br/>
    /// While this is very useful for microbenchmarking each part (as they all
    /// appear separately in the profiler), this has a lot of overhead from the
    /// defensive copies Unity makes before every job.
    /// </summary>
    /// <remarks>
    /// This is the commented reference implementation. All other strategies
    /// will not contain comments unless it's particular to that strategy.
    /// <br/>
    /// For instance, the generic unrolling is necessary in each, but I'll only
    /// comment it here.
    /// </remarks>
    internal class TickStrategySeparated<TGameInput>
        : ITickStrategy<TGameInput>
        where TGameInput : unmanaged, IGameInput {

        public JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency, int ticks) {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Can only tick a positive number of times.");
#if UNITY_EDITOR
            // See the "TODO: NOTE: OBNOXIOUS:" comment below.
            if (ticks == 1)
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = true;
            else
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = false;
#endif

            ref var handle = ref dependency;
            for(int i = 0; i < ticks; i++) {
                handle = ScheduleTick(scene, handle);
            }
            return handle;
        }

        public unsafe JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency) {
            // WARNING: EVERY .Schedule() HERE MUST HAVE A HANDLE ARG
            // YOU"RE DOING IT WRONG OTHERWISE
            // You don't notice it until you run it at [ridiculus]x speed in
            // the player, but then you just freeze and all threads are Burst
            // and waiting. Notice that behaviour? This is the problem.

            // NOTE: CombineDependencies may cause jobs to run already, as in
            // JobHandle.ScheduleBatchedJobs().
            // Look into the performance implications of this.
            ref JobHandle handle = ref dependency;

            // Run VMs
            // We cannot assume how many VMs there actually are, so schedule a
            // predictable, always-non-zero number of jobs.
            // Note: Scheduling X jobs with X available worker threads that are
            // all idle does not actually guarantee an even distribution of
            // work, but in the player the workers are saturated anyway so who
            // gives.
            int concurrentVMs = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;
            concurrentVMs = math.max(1, concurrentVMs);
            NativeArray<JobHandle> vmHandles = new(concurrentVMs, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < concurrentVMs; i++) {
                vmHandles[i] = new VMJobMany() {
                    vms = scene.activeVMs,
                    jobIndex = i,
                    totalJobs = concurrentVMs
                }.Schedule(handle);
            }
            handle = JobHandle.CombineDependencies(vmHandles);
            vmHandles.Dispose();

            // TODO: Message handling, detecting and disabling VMs with zero
            // bullets. Unfortunately, both of these operations are sequental :(
            handle = new VMsCommandsJob() {
                field = scene.bulletField,
                player = scene.player,
                vms = scene.activeVMs,
                createdBullets = scene.createdBullets
            }.Schedule(handle);

            // Update positions
            var moveHandle1 = new MoveBulletsJob(in scene.bulletField).Schedule(handle);
            var moveHandle2 = ScheduleMovePlayerJob(scene, handle);
            handle = JobHandle.CombineDependencies(moveHandle1, moveHandle2);

            // Check player collisions (note that bullets may be deleted already)
            var collideHandle1 = new BulletCollisionJob(
                in scene.bulletField,
                in scene.playerHitbox,
                scene.playerHitboxResult
            ).Schedule(handle);
            var collideHandle2 = new BulletCollisionJob(
                in scene.bulletField,
                in scene.playerGrazebox,
                scene.playerGrazeboxResult
            ).Schedule(handle);
            handle = JobHandle.CombineDependencies(collideHandle1, collideHandle2);

            // Post-process deletions
            // TODO: NOTE: OBNOXIOUS: for both of these jobs, the jobs debugger
            // suddenly takes up an exponential amount of time verifying the
            // dependency graph. Scheduling 1 iteration, fine, 30 takes up the
            // entire frame, 60 crashes Unity.
            // The workaround: in the editor, toggle Jobs > JobsDebugger.
            // The ScheduleTick(int, JobHandle) overload automatically enables
            // it when doing one tick and disables it when doing multiple ticks.
            // This disables the race condition check however.
            // Disabled and I can run 500 iterations a frame no problem.
            handle = new PostProcessPlayerCollisionJob(
                scene.player, in scene.bulletField,
                scene.playerHitboxResult, scene.playerGrazeboxResult
            ).Schedule(handle);
            handle = new PostProcessDeletionsJob(in scene.bulletField).Schedule(handle);

            // Prepare the next frame
            handle = new IncrementTickJob(
                scene.gameTick,
                (int*)scene.input.GetUnsafeTypedPtr()
            ).Schedule(handle);

            return handle;
        }

        // Burst can _only_ compile generic jobs whose constructor is of the
        // form `MyJob<ExplicitType>`. Constructors of the form `MyJob<T>` for
        // some ambient generic T type won't work.  `TGameInput` is like that,
        // so...
        // Unroll the generic into explicit types. :(
        unsafe JobHandle ScheduleMovePlayerJob(DanmakuScene<TGameInput> scene, JobHandle deps) {
            // Also, I _would_ use those nice is/switch expressions, but...
            // https://sharplab.io/#v2:D4AQzABAzgLgTgVwMYwgWQDwGkB8EDeAsAFARkTgQCWAdqlANwnkWRoAUmuEAtgJQFmLclAgBeXgDpGQsgF8SC4iUogATBADC2PEVLkueKk2X6ytVADF2AsUegB3KjCQALQWeGYLRjXYg0AKYO6Bg+7FRqfNIANLLCAPrieEEhhhHRUPFyJnJAA=
            // They box. This is a hot loop. Ffs.
            // Even uglier manual pointer shit it is.
            TGameInput* t = scene.input.GetUnsafeTypedPtr();
            if (typeof(TGameInput) == typeof(KeyboardInput)) {
                return new MovePlayerJob<KeyboardInput>(
                    in scene.player, (KeyboardInput*)t, in scene.playerHitbox, in scene.playerGrazebox
                ).Schedule(deps);
            }

            // If other, fall back to non-bursted code. The only downside is
            // the performance, which is much more acceptable than crashing.
            // Easily recognisable by large blue blobs instead of thin green
            // blobs in Unity's profiler.
            return new MovePlayerJob<TGameInput>(
                in scene.player, t, in scene.playerHitbox, in scene.playerGrazebox
            ).Schedule(deps);
        }
    }
}