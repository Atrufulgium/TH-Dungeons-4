using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies {

    /// <summary>
    /// Represents a specific way to schedule one or multiple gameticks taking
    /// place in a <see cref="DanmakuScene"/>.
    /// <br/>
    /// You can consider <see cref="TickStrategySeparated{TGameInput}"/> as a
    /// sort-of reference implementation all other implementations need to be
    /// synced with.
    /// </summary>
    public interface ITickStrategy<TGameInput> where TGameInput : unmanaged, IGameInput {

        /// <summary>
        /// Add a gametick of processing <paramref name="scene"/> after <paramref name="dependency"/>.
        /// </summary>
        public JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency);

        /// <summary>
        /// Add <paramref name="ticks"/> gameticks of processing <paramref name="scene"/>
        /// after <paramref name="dependency"/>.
        /// </summary>
        public JobHandle ScheduleTick(DanmakuScene<TGameInput> scene, JobHandle dependency, int ticks);
    }
}