﻿using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {
    /// <summary>
    /// This job is responsible for running many VMs.
    /// <br/>
    /// The vm list is split up into chunks.
    /// E.g. if you have 10 vms and 3 totaljobs, the workload may be split up
    /// as [1,2,3,4] [5,6,7] [8,9,10].
    /// <br/>
    /// Us manually doing this split goes straight against Unity's safety
    /// system. However, each index only affects itself here, so there is no
    /// chance for race conditions or deadlocks to occur.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct VMJobMany : IJob {

        [NativeDisableContainerSafetyRestriction]
        public NativeList<VM> vms;
        // ∈ [0, totalJobs-1]
        public int jobIndex;
        public int totalJobs;

        public unsafe readonly void Execute() {
            VMPassMany.Execute(in vms, in jobIndex, in totalJobs);
        }
    }

    internal static class VMPassMany {
        public static unsafe void Execute(
            in NativeList<VM> vms,
            in int jobIndex,
            in int totalJobs
        ) {
            // x: lower, inclusive; y: upper, exclusive
            int2 range = (int2)math.floor(vms.Length * new float2(jobIndex, jobIndex + 1) / totalJobs);
            // While the above is guaranteed to go right for 0, I don't trust
            // floats enough to be sure that the last vm is not skipped.
            if (jobIndex == totalJobs - 1)
                range.y = vms.Length;

            // In general, the underlying pointer for lists may move around.
            // Luckily, we do not resize this list during this, so that is not
            // a problem.
            var ptr = vms.GetUnsafeTypedPtr();
            for (int i = range.x; i < range.y; i++)
                (ptr + i)->RunMain();
        }
    }
}
