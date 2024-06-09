using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletField;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {
    /// <summary>
    /// Stores a list of template VMs that can be copied when needed later.
    /// </summary>
    public struct VMList : IDisposable {
        NativeParallelHashMap<FixedString128Bytes, VM> templateVMs;

        // The VMs need pointers to native memory for their opcode and rngs.
        // This is obnoxious but oh well.
        // Just allocate a *massive* block with wraparound to use for this space.
        // Should be large enough that there's no way for a VM to live long
        // enough that its pointer gets snatched again.
        // (Note that `Random` is also 4 bytes.)
        // TODO: use a better memory model lol, free lists exist.
        const int MASSIVE = 1024 * 1024;
        NativeArray<uint> uintBlock;
        NativeArray<Unity.Mathematics.Random> rngBlock;
        NativeReference<int> index;

        /// <summary>
        /// Puts <i>copies</i> of <paramref name="vms"/> into a VMList.
        /// </summary>
        public unsafe VMList(string[] keys, VM[] vms) {
            if (keys.Length != vms.Length)
                throw new ArgumentOutOfRangeException(nameof(vms), "The keys and vms array aren't the same size.");

            uintBlock = new(MASSIVE, Allocator.Persistent);
            rngBlock = new(MASSIVE, Allocator.Persistent);
            index = new(0, Allocator.Persistent);

            templateVMs = new(64, Allocator.Persistent);
            for (int i = 0; i < keys.Length; i++) {
                GetPointers(uintBlock, rngBlock, index, out var uintRef, out var rngRef);
                templateVMs.Add(new(keys[i]), new(vms[i], default, uintRef, rngRef));
            }
        }

        public void Add(string key, ref VM vm)
            => Add(new FixedString128Bytes(key), ref vm);

        public unsafe void Add(FixedString128Bytes key, ref VM vm) {
            GetPointers(out var uintRef, out var rngRef);
            templateVMs.Add(new(key), new(vm, default, uintRef, rngRef));
        }

        public VM GetCopy(string key, UnsafeList<BulletReference> affectedBullets)
            => GetCopy(new FixedString128Bytes(key), affectedBullets);

        public unsafe VM GetCopy(FixedString128Bytes key, UnsafeList<BulletReference> affectedBullets) {
            GetPointers(out var uintRef, out var rngRef);
            return new VM(templateVMs[key], affectedBullets, uintRef, rngRef);
        }

        unsafe void GetPointers(out uint* uintRef, out Unity.Mathematics.Random* rngRef)
            => GetPointers(uintBlock, rngBlock, index, out uintRef, out rngRef);

        static unsafe void GetPointers(
            NativeArray<uint> uintBlock,
            NativeArray<Unity.Mathematics.Random> rngBlock,
            NativeReference<int> index,
            out uint* uintRef,
            out Unity.Mathematics.Random* rngRef
        ) {
            uintRef = uintBlock.GetUnsafeTypedPtr() + index.Value;
            rngRef = rngBlock.GetUnsafeTypedPtr() + index.Value;
            index.Value = (index.Value + 1) % MASSIVE;
        }

        public void Dispose() {
            foreach (var kv in templateVMs) {
                kv.Value.Dispose();
            }
            templateVMs.Dispose();
            uintBlock.Dispose();
            rngBlock.Dispose();
            index.Dispose();
        }
    }
}
