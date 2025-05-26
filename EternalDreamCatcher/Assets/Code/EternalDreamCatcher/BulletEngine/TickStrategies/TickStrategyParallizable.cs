using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies {
    /// <summary>
    /// A strategy that first schedules all VM jobs on their own threads, and
    /// then does all the post processing in one massive job.
    /// <br/>
    /// Whether you want to use this or <see cref="TickStrategyMegajob{TGameInput}"/>
    /// depends on how heavy the VMs are. It might be possible to do a hybrid
    /// strategy based on real-time data.
    /// </summary>
    internal class TickStrategyParallizable : ITickStrategy {

        public JobHandle ScheduleTick(DanmakuScene scene, JobHandle dependency, int ticks) {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Can only tick a positive number of times.");
#if UNITY_EDITOR
            if (ticks == 1)
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = true;
            else
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = false;
#endif

            ref var handle = ref dependency;
            for (int i = 0; i < ticks; i++) {
                handle = ScheduleTick(scene, handle);
            }
            return handle;
        }

        public JobHandle ScheduleTick(DanmakuScene scene, JobHandle dependency) {
            ref JobHandle handle = ref dependency;

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

            return new PostVMGameTickJob(
                    in scene.bulletField,
                    in scene.player,
                    in scene.activeVMs,
                    in scene.createdBullets,
                    in scene.input,
                    in scene.playerHitbox,
                    in scene.playerGrazebox,
                    in scene.playerHitboxResult,
                    in scene.playerGrazeboxResult,
                    in scene.gameTick
            ).Schedule(handle);
        }
    }
}