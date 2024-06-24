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
        /// <summary> The layer determines draw order. </summary>
        internal NativeArray<int> layer;
        /// <summary> The usually-outer color of a bullet. </summary>
        internal NativeArray<float4> outerColor;
        /// <summary> The usually-inner color of a bullet. </summary>
        internal NativeArray<float4> innerColor;
        /// <summary> An arbitrary scale factor for bullet size. </summary>
        internal NativeArray<float> renderScale;
        #endregion

        /// <summary>
        /// What index this bullet shifted to due to <see cref="FinalizeDeletion"/>:
        /// a value of `t` indicates that this index was moved to index `t-1`.
        /// <br/>
        /// A value of `0` signifies "this bullet was deleted or invalid." A
        /// value of `-1` signifies the same, but is specifically the value of
        /// `deleteShifts[old Active]`.
        /// </summary>
        private NativeArray<int> deleteShifts;
        private NativeReference<int> deletedCount;

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
            outerColor = new(MAX_BULLETS, Allocator.Persistent);
            innerColor = new(MAX_BULLETS, Allocator.Persistent);
            renderScale = new(MAX_BULLETS, Allocator.Persistent);
            deleteShifts = new(MAX_BULLETS, Allocator.Persistent);
            deletedCount = new(0, Allocator.Persistent);
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
            outerColor[a] = bullet.outerColor;
            innerColor[a] = bullet.innerColor;
            renderScale[a] = bullet.renderScale;

            active.Value++;

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

            deleteShifts.Clear();

            // Compactify by removing the gaps of `deletedReferences`.
            // Unfortunately, we care about order, so we cannot do the O(1)
            // thing and simply have to move everything over to fill up the
            // gaps.
            var newActive = Active - deletedCount.Value;
            int writeIndex = 0;
            int readIndex = 0;
            for (; writeIndex < newActive; writeIndex++, readIndex++) {
                while (Hint.Unlikely(valid.GetBits(readIndex) == 0)) {
                    if (!BurstUtils.IsBurstCompiled) {
                        Assert.IsTrue(readIndex < Active);
                    }
                    readIndex++;
                }
                Overwrite(readIndex, writeIndex);
                deleteShifts[readIndex] = writeIndex + 1;
            }

            // Don't forget to invalidate the rest of the range.
            valid.SetBits(writeIndex, false, Active - newActive);
            if (Active + 1 < MAX_BULLETS)
                deleteShifts[Active + 1] = -1;

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
                int to = deleteShifts[from] - 1;
                if (Hint.Unlikely(to == -2))
                    break;
                if (Hint.Unlikely(to == -1))
                    continue;
                if (from == to)
                    continue;
                yield return ((BulletReference)from, (BulletReference)to);
            }
        }

        /// <summary>
        /// <see cref="FinalizeDeletion"/> moves around the bullets in the
        /// underlying array. This allows you to track bullets through those
        /// updates.
        /// </summary>
        public BulletReference GetFinalizeDeletionShift(BulletReference from) {
            int to = deleteShifts[(int)from] - 1;
            if (to < 0)
                throw new ArgumentException("This bullet was deleted or not valid to begin with.");
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
            outerColor.Dispose();
            innerColor.Dispose();
            renderScale.Dispose();
            deleteShifts.Dispose();
            deletedCount.Dispose();
        }
    }
}
