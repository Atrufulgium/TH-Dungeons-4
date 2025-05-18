using Atrufulgium.EternalDreamCatcher.Base;
using System;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// Stores unsafe access to all bullets.
    /// <br/>
    /// You <b>must</b> use the <see cref="BulletField(bool)"/> constructor when
    /// creating this struct.
    /// <br/>
    /// This type is blittable.
    /// </summary>
    // This is SoA-shaped instead of AoS-shaped for SIMD purposes.
    // Unfortunately, c# doesn't like that look.
    // The layout of the arrays is as follows:
    // 0 [long list of bullets] Active [junk] MAX_BULLETS
    // At first I wanted to put each bullet type in their own bin, but ehh...
    // That's obnoxious.
    // Now FieldRenderer does the binning instead. It's fast.
    public struct BulletField : IDisposable {

        // Must be a multiple of 4 and not exceed ushort.maxValue+1.
        public const int MAX_BULLETS = 4096;

        /// <inheritdoc cref="active"/>
        public int Active => active.Value;
        /// <summary>
        /// How many bullets are valid.
        /// </summary>
        internal NativeReference<int> active;

        // WHENEVER ADDING AN ARRAY:
        // - Obviously do something with BulletCreationparams.
        // - Add it to the constructor
        // - Modify OverWrite(int, int)
        // - Modify Dispose()

        #region basic properties
        /// <summary> For valid bullets, stores the x position. Undefined for invalid bullets. </summary>
        internal NativeArray<float> x;
        /// <summary> For valid bullets, stores the y position. Undefined for invalid bullets. </summary>
        internal NativeArray<float> y;
        /// <summary> Whether this bullet is valid. </summary>
        internal NativeBitArray valid;

        /// <summary> Miscellaneous collection of properties together in one flags enum. </summary>
        internal NativeArray<MiscBulletProps> prop;
        #endregion

        #region movement
        /// <summary> Stores the movement in the x component each gametick. </summary>
        internal NativeArray<float> dx;
        /// <summary> Stores the movement in the y component each gametick. </summary>
        internal NativeArray<float> dy;
        #endregion

        #region gameplay stuff
        /// <summary> Stores the hitbox size of each bullet. </summary>
        internal NativeArray<float> radius;
        #endregion

        #region rendering stuff
        /// <summary> The index in <see cref="BulletFieldRenderer.bulletTextures"/>. </summary>
        internal NativeArray<int> textureID;
        /// <summary> The layer determines coarse draw order. </summary>
        internal NativeArray<int> layer;
        /// <summary> The ID determines fine draw order. </summary>
        internal NativeArray<uint> id;
        /// <summary> The usually-outer color of a bullet. </summary>
        internal NativeArray<float4> outerColor;
        /// <summary> The usually-inner color of a bullet. </summary>
        internal NativeArray<float4> innerColor;
        /// <summary> An arbitrary scale factor for bullet size. </summary>
        internal NativeArray<float> renderScale;
        #endregion

        /// <summary>
        /// What index this bullet shifted to due to <see cref="FinalizeDeletion"/>:
        /// a value of `t` at index `i` indicates that this index was moved to `i-t`.
        /// <br/>
        /// A value of `65230` signifies "this bullet was deleted or invalid." A
        /// value of `65231` signifies the same, but is specifically the value of
        /// `deleteShifts[old Active]`.
        /// </summary>
        private NativeArray<ushort> deleteShifts;
        private NativeReference<ushort> deletedCount;
        private const ushort DELETED = 65230;
        private const ushort DELETE_BOUNDARY = 65231;

        private NativeReference<uint> nextID;

        public BulletField(bool validConstructor = true) {
            active = new(0, Allocator.Persistent);
            x = new(MAX_BULLETS, Allocator.Persistent);
            y = new(MAX_BULLETS, Allocator.Persistent);
            valid = new(MAX_BULLETS, Allocator.Persistent);
            prop = new(MAX_BULLETS, Allocator.Persistent);
            dx = new(MAX_BULLETS, Allocator.Persistent);
            dy = new(MAX_BULLETS, Allocator.Persistent);
            radius = new(MAX_BULLETS, Allocator.Persistent);
            textureID = new(MAX_BULLETS, Allocator.Persistent);
            layer = new(MAX_BULLETS, Allocator.Persistent);
            id = new(MAX_BULLETS, Allocator.Persistent);
            outerColor = new(MAX_BULLETS, Allocator.Persistent);
            innerColor = new(MAX_BULLETS, Allocator.Persistent);
            renderScale = new(MAX_BULLETS, Allocator.Persistent);
            deleteShifts = new(MAX_BULLETS, Allocator.Persistent);
            deletedCount = new(0, Allocator.Persistent);
            nextID = new(0, Allocator.Persistent);
        }

        /// <summary>
        /// Create a bullet, and return a reference to it. If the bullet cap is
        /// reached, returns <c>null</c> instead.
        /// </summary>
        public BulletReference? CreateBullet(ref BulletCreationParams bullet) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!x.IsCreated)
                throw new InvalidOperationException("This Field was created with `new Field()` or `default`. This is not supported; Fields must be created with `new Field(true)`.");
#endif
            int a = active.Value;
            if (a == MAX_BULLETS)
                return null;
            x[a] = bullet.spawnPosition.x;
            y[a] = bullet.spawnPosition.y;
            valid.Set(a, true);
            prop[a] = bullet.bulletProps;
            dx[a] = bullet.movement.x;
            dy[a] = bullet.movement.y;
            radius[a] = bullet.hitboxSize;
            textureID[a] = bullet.textureID;
            layer[a] = bullet.layer;
            id[a] = nextID.Value;
            outerColor[a] = bullet.outerColor;
            innerColor[a] = bullet.innerColor;
            renderScale[a] = bullet.renderScale;

            active.Value++;
            nextID.Value++;

            return (BulletReference)a;
        }

        /// <summary>
        /// If it exists, deletes bullet <paramref name="reference"/>.
        /// <br/>
        /// If it does not exist, throws <see cref="ArgumentOutOfRangeException"/>.
        /// <br/>
        /// <b>Warning:</b> Between the first call to this, and the call to
        /// <see cref="FinalizeDeletion"/>, the active bullets do *not* take up
        /// a contiguous range of the array.
        /// </summary>
        public unsafe void DeleteBullet(BulletReference reference) {
            int i = (int)reference;
            if (valid.GetBits(i) == 0)
                return;
            if (i >= active.Value || i < 0)
                throw new ArgumentOutOfRangeException(nameof(i), $"Requested deletion of bullet {reference}. The valid range is currently `[0, {Active-1}]`, inclusive.");
            valid.Set(i, false);
            deletedCount.Value++;
        }

        /// <summary>
        /// Filters a list of bullets to contain only the bullets that have
        /// been deleted since the last <see cref="FinalizeDeletion"/> call,
        /// typically meaning "last frame".
        /// </summary>
        public IEnumerable<BulletReference> FilterDeleted(IEnumerable<BulletReference> bullets) {
            foreach (var b in bullets)
                if (valid.GetBits((int)b) == 0)
                    yield return b;
        }

        /// <summary>
        /// Whether a bullet reference has been deleted since the last
        /// <see cref="FinalizeDeletion"/> call, typically meaning "last frame".
        /// </summary>
        public bool ReferenceIsDeleted(BulletReference bullet)
            => valid.GetBits((int)bullet) == 0;

        /// <summary>
        /// Clears the deleted bullet range, and allows new bullets to be
        /// created in those indices.
        /// <br/>
        /// This moves bullets around. This movement can be read off from
        /// <see cref="GetFinalizeDeletionShifts"/>.
        /// </summary>
        public void FinalizeDeletion() {
            if (deletedCount.Value == 0)
                return;

            // By default, mark "didn't move" and "not deleted".
            deleteShifts.Clear(Active);

            // Compactify by removing the gaps of `deletedReferences`.
            // Unfortunately, this messes with the order, so swapping bullets
            // around like this means the renderer has to a little more work.
            int newActive = Active - deletedCount.Value;
            int backIndex = Active - 1;
            int frontIndex = 0;
            
            while (Hint.Likely(frontIndex < newActive)) {
                // Note: _Supposedly_ `GetBits` does not throw if "`pos + numBits`
                // exceeds the length of the array".
                // It does though. Lmao.
                // Either way, we can skip huge chunks at a time, which is common
                // due to how rare bullet deletions are.
                ulong bits = valid.GetBits(
                    frontIndex,
                    math.min(64, MAX_BULLETS - frontIndex)
                );

                // We can skip over all 1-bits to the next 0-bit.
                // (Note that math.tzcnt(0) gives 64.)
                var validCount = math.tzcnt(~bits);
                frontIndex += validCount;
                if (Hint.Likely(validCount == 64)) {
                    continue;
                }
                // (Keep the invariant "frontIndex < newActive" going)
                if (Hint.Unlikely(frontIndex >= newActive)) {
                    break;
                }

                // We're on a deleted bullet in [0,newActive).
                // Grab a bullet [newActive,Active) to put there, searching
                // from the back.
                // This bullet must exist (as otherwise the Active/newActive
                // bookkeeping went wrong somewhere).

                // Swap an invalid bullet with the last currently valid bullet
                while (Hint.Likely(backIndex > frontIndex)) {
                    // First do a sweeping larger-scale search.
                    // If we can't do the larger-scale search, do a smol one,
                    // step by step.
                    if (Hint.Unlikely(backIndex < newActive + 64)) {
                        while (backIndex > newActive && valid.GetBits(backIndex) == 0) {
                            backIndex--;
                        }
                        break;
                    }

                    // We can do a large-scale search!
                    // These are generally unlikely due to how rare bullet deletions are.
                    var backBits = valid.GetBits(backIndex - 63, 64);
                    // (Fount from the other direction because we're going backwards)
                    var invalidCount = math.lzcnt(backBits);
                    backIndex -= invalidCount;
                }

                if (!BurstUtils.IsBurstCompiled) {
                    Assert.IsTrue(valid.GetBits(backIndex) == 1, "Trying to grab a deleted bullet.");
                    Assert.IsTrue(backIndex > frontIndex, "Trying to put a bullet _back_.");
                }

                // We can actually move the boolet from back to front
                Overwrite(backIndex, frontIndex);
                valid.Set(backIndex, false);
                deleteShifts[backIndex] = (ushort)(backIndex - frontIndex);
                deleteShifts[frontIndex] = DELETED;

                frontIndex++;
                backIndex--;
            }

            // Don't forget to invalidate the rest of the range.
            valid.SetBits(newActive, false, Active - newActive);
            if (Active + 1 < MAX_BULLETS)
                deleteShifts[Active + 1] = DELETE_BOUNDARY;

            active.Value = newActive;
            deletedCount.Value = 0;
        }

        /// <summary>
        /// <see cref="FinalizeDeletion"/> moves around the bullets in the
        /// underlying array. This allows you to track bullets through those
        /// updates.
        /// <br/>
        /// For each tuple entry, `from` describes the reference before the
        /// deletions, and `to` describes the updated positions. Each `to`
        /// points to a valid, active bullet.
        /// <br/>
        /// All other (deleted) references should have been handled with one of
        /// <list type="bullet">
        /// <item><see cref="ReferenceIsDeleted(BulletReference)"/></item>
        /// <item><see cref="FilterDeleted(IEnumerable{BulletReference})"/></item>
        /// </list>
        /// already.
        /// </summary>
        public IEnumerable<(BulletReference from, BulletReference to)> GetFinalizeDeletionShifts() {
            for (int from = 0; from < MAX_BULLETS; from++) {
                int offset = deleteShifts[from];
                if (Hint.Unlikely(offset == DELETE_BOUNDARY))
                    break;
                if (Hint.Unlikely(offset == DELETED))
                    continue;
                if (offset == 0)
                    continue;
                int to = from - offset;
                yield return ((BulletReference)from, (BulletReference)to);
            }
        }

        /// <summary>
        /// <see cref="FinalizeDeletion"/> moves around the bullets in the
        /// underlying array. This allows you to track bullets through those
        /// updates.
        /// </summary>
        public BulletReference GetFinalizeDeletionShift(BulletReference from) {
            int offset = deleteShifts[(int)from];
            if (offset == DELETED || offset == DELETE_BOUNDARY)
                throw new ArgumentException("This bullet was deleted or not valid to begin with.");
            int to = (int)from - offset;
            return (BulletReference)to;
        }

        private void Overwrite(int i, int j) {
            Overwrite(i, j, x);
            Overwrite(i, j, y);
            valid.Set(j, valid.GetBits(i) != 0);
            Overwrite(i, j, prop);
            Overwrite(i, j, dx);
            Overwrite(i, j, dy);
            Overwrite(i, j, textureID);
            Overwrite(i, j, layer);
            Overwrite(i, j, id);
            Overwrite(i, j, outerColor);
            Overwrite(i, j, innerColor);
            Overwrite(i, j, renderScale);
        }

        private static void Overwrite<T>(int i, int j, NativeArray<T> arr) where T : struct {
            arr[j] = arr[i];
        }

        public void Dispose() {
            active.Dispose();
            x.Dispose();
            y.Dispose();
            valid.Dispose();
            prop.Dispose();
            dx.Dispose();
            dy.Dispose();
            radius.Dispose();
            textureID.Dispose();
            layer.Dispose();
            id.Dispose();
            outerColor.Dispose();
            innerColor.Dispose();
            renderScale.Dispose();
            deleteShifts.Dispose();
            deletedCount.Dispose();
        }
    }
}
