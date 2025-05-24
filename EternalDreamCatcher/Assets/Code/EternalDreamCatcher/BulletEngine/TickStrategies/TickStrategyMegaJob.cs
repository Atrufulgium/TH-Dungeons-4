using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies {
    /// <summary>
    /// A strategy that schedules each part of the pipeline in one huge job.
    /// <br/>
    /// Unlike <see cref="TickStrategyParallizable{TGameInput}"/>, this job
    /// schedules multiple ticks into one huge job.
    /// <br/>
    /// This removes most of the copy overhead from the Jobs System, but also
    /// only runs the VMs on one thread. If this turns out to be a problem, use
    /// <see cref="TickStrategyParallizable{TGameInput}"/> instead.
    /// </summary>
    internal class TickStrategyMegaJob<TGameInput>
        : ITickStrategy<TGameInput>
        where TGameInput : unmanaged, IGameInput {

        public JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency)
            => ScheduleTick(scene, dependency, 1);

        public unsafe JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency, int ticks) {
            return ScheduleEntireGameTickJob(scene, dependency, ticks);
        }

        unsafe JobHandle ScheduleEntireGameTickJob(DanmakuScene<TGameInput> scene, JobHandle deps, int ticks) {
            TGameInput* t = scene.input.GetUnsafeTypedPtr();
            if (typeof(TGameInput) == typeof(KeyboardInput)) {
                return new EntireGameTickJob<KeyboardInput>(
                    in scene.bulletField,
                    in scene.player,
                    in scene.activeVMs,
                    in scene.createdBullets,
                    (KeyboardInput*)t,
                    in scene.playerHitbox,
                    in scene.playerGrazebox,
                    in scene.playerHitboxResult,
                    in scene.playerGrazeboxResult,
                    in scene.gameTick,
                    in ticks
                ).Schedule(deps);
            }

            return new EntireGameTickJob<TGameInput>(
                    in scene.bulletField,
                    in scene.player,
                    in scene.activeVMs,
                    in scene.createdBullets,
                    t,
                    in scene.playerHitbox,
                    in scene.playerGrazebox,
                    in scene.playerHitboxResult,
                    in scene.playerGrazeboxResult,
                    in scene.gameTick,
                    in ticks
            ).Schedule(deps);
        }
    }
}