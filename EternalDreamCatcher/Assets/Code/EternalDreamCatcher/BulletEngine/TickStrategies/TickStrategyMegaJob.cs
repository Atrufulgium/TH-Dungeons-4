using System;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies {
    /// <summary>
    /// A strategy that schedules each part of the pipeline in one huge job.
    /// <br/>
    /// Unlike <see cref="TickStrategyParallizable"/>, this job
    /// schedules multiple ticks into one huge job.
    /// <br/>
    /// This removes most of the copy overhead from the Jobs System, but also
    /// only runs the VMs on one thread. If this turns out to be a problem, use
    /// <see cref="TickStrategyParallizable"/> instead.
    /// </summary>
    internal class TickStrategyMegaJob : ITickStrategy {

        public JobHandle ScheduleTick(DanmakuScene scene, JobHandle dependency)
            => ScheduleTick(scene, dependency, 1);

        public JobHandle ScheduleTick(DanmakuScene scene, JobHandle dependency, int ticks) {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Can only tick a positive number of times.");

            return new EntireGameTickJob(
                    in scene.bulletField,
                    in scene.player,
                    in scene.activeVMs,
                    in scene.createdBullets,
                    in scene.input,
                    in scene.playerHitbox,
                    in scene.playerGrazebox,
                    in scene.playerHitboxResult,
                    in scene.playerGrazeboxResult,
                    in scene.gameTick,
                    in ticks
            ).Schedule(dependency);
        }
    }
}