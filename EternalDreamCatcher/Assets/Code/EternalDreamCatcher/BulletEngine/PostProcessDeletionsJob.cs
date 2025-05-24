using Unity.Burst;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Calls <see cref="BulletField.FinalizeDeletion"/>.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct PostProcessDeletionsJob : IJob {
        public BulletField field;
        public PostProcessDeletionsJob(in BulletField field) => this.field = field;
        public void Execute() => PostProcessDeletionsPass.Execute(ref field);
    }

    /// <inheritdoc cref="PostProcessDeletionsJob"/>
    internal static class PostProcessDeletionsPass {
        public static void Execute(ref BulletField field) => field.FinalizeDeletion();
    }
}
