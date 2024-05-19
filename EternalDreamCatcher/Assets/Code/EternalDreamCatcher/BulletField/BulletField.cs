using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {

    /// <summary>
    /// Stores unsafe access to all bullets.
    /// </summary>
    // This is SoA-shaped instead of AoS-shaped for SIMD purposes.
    // Unfortunately, c# doesn't like that look.
    // The layout of the arrays is as follows:
    // 0 [long list of bullets] Active [junk] MAX_BULLETS
    // At first I wanted to put each bullet type in their own bin, but ehh...
    // That's obnoxious.
    // Now FieldRenderer does the binning instead. It's fast.
    public class Field : IDisposable {

        // Must be a multiple of 4 and not exceed ushort.maxValue.
        public const int MAX_BULLETS = 4096;

        /// <inheritdoc cref="active"/>
        public int Active => active;
        /// <summary>
        /// How many bullets are valid.
        /// </summary>
        int active = 0;

        // WHENEVER ADDING AN ARRAY:
        // - Obviously do something with BulletCreationparams.
        // - Modify OverWrite(int, int)
        // - Modify Dispose()

        #region basic properties
        /// <summary> For valid bullets, stores the x position. Undefined for invalid bullets. </summary>
        internal NativeArray<float> x = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> For valid bullets, stores the y position. Undefined for invalid bullets. </summary>
        internal NativeArray<float> y = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> For valid bullets, stores the z position. Undefined for invalid bullets. </summary>
        // Ideally this one isn't needed, but as we're going to be swapping
        // indices around, relying on order is not sufficient.
        internal NativeArray<float> z = new(MAX_BULLETS, Allocator.Persistent);
        float currentZ = 0;
        #endregion

        #region movement
        /// <summary> Stores the movement in the x component each gametick. </summary>
        internal NativeArray<float> dx = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> Stores the movement in the y component each gametick. </summary>
        internal NativeArray<float> dy = new(MAX_BULLETS, Allocator.Persistent);
        #endregion

        #region gameplay stuff
        /// <summary> Stores the hitbox size of each bullet. </summary>
        internal NativeArray<float> radius = new(MAX_BULLETS, Allocator.Persistent);
        #endregion

        #region rendering stuff
        /// <summary> The index in <see cref="FieldRenderer.bulletTextures"/>. </summary>
        internal NativeArray<int> textureID = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> The layer determines draw order. </summary>
        internal NativeArray<int> layer = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> The usually-outer color of a bullet. </summary>
        internal NativeArray<float4> outerColor = new(MAX_BULLETS, Allocator.Persistent);
        /// <summary> The usually-inner color of a bullet. </summary>
        internal NativeArray<float4> innerColor = new(MAX_BULLETS, Allocator.Persistent);
        #endregion

        readonly internal HashSet<BulletReference> deletedReferences = new(4096);
        readonly internal HashSet<BulletReference> processedDeletedReferences = new(4096);

        /// <summary>
        /// Create a bullet, and return a reference to it. If the bullet cap is
        /// reached, returns <c>null</c> instead.
        /// </summary>
        public BulletReference? CreateBullet(ref BulletCreationParams bullet) {
            if (active == MAX_BULLETS)
                return null;
            x[active] = bullet.spawnPosition.x;
            y[active] = bullet.spawnPosition.y;
            z[active] = currentZ;
            dx[active] = bullet.movement.x;
            dy[active] = bullet.movement.y;
            radius[active] = bullet.hitboxSize;
            textureID[active] = bullet.textureID;
            layer[active] = bullet.layer;
            outerColor[active] = bullet.outerColor;
            innerColor[active] = bullet.innerColor;

            active++;
            currentZ += 0.000001f;
            if (currentZ >= 0.9f)
                currentZ = 0;

            return (BulletReference)active - 1;
        }

        /// <summary>
        /// If it exists, deletes the bullet at index <paramref name="i"/>.
        /// <br/>
        /// Otherwise, throws <see cref="ArgumentOutOfRangeException"/>.
        /// <br/>
        /// <b>Warning:</b> Between the first call to this, and the call to
        /// <see cref="FinalizeDeletion"/>, the active bullets do *not* take up
        /// a contiguous range of the array.
        /// </summary>
        public void DeleteBullet(BulletReference reference) {
            if (deletedReferences.Contains(reference))
                return;
            int i = (int)reference;
            if (i >= active || i < 0)
                throw new ArgumentOutOfRangeException(nameof(i), $"Requested deletion of bullet {reference}. The valid range is currently `[0, {active-1}]`, inclusive.");
            deletedReferences.Add(reference);
        }

        /// <summary>
        /// Filters a list of bullets to contain only the bullets that have
        /// been deleted since the last <see cref="FinalizeDeletion"/> call,
        /// typically meaning "last frame".
        /// </summary>
        public IEnumerable<BulletReference> FilterDeleted(IEnumerable<BulletReference> bullets) {
            foreach (var b in bullets)
                if (deletedReferences.Contains(b))
                    yield return b;
        }

        /// <summary>
        /// Whether a bullet reference has been deleted since the last
        /// <see cref="FinalizeDeletion"/> call, typically meaning "last frame".
        /// </summary>
        public bool ReferenceIsDeleted(BulletReference bullet)
            => deletedReferences.Contains(bullet);

        /// <summary>
        /// Clears the deleted bullet range, and allows new bullets to be
        /// created in those indices.
        /// </summary>
        public void FinalizeDeletion() {
            // Compactify by removing the gaps of `deletedReferences`.
            // Do this by repeatedly moving the last active bullet into the
            // gap.
            // (This last active bullet may have also been deleted, so some
            //  care is needed.)
            processedDeletedReferences.Clear();
            foreach (var b in deletedReferences) {
                if (processedDeletedReferences.Contains(b))
                    continue;

                var overwritten = b;
                var reference = (BulletReference)active - 1;

                // (For easier understanding, ignore this while loop on first
                //  reading -- this is the "some care" part mentioned above.)
                while (active > 0) {
                    if (deletedReferences.Contains(reference)) {
                        // No need to overwrite as it's at the "end" already.
                        // Only consume the "active" and add as processed.
                        active--;
                        processedDeletedReferences.Add(reference);
                        reference = (BulletReference)active - 1;
                    } else {
                        break;
                    }
                }
                
                Overwrite(reference, overwritten);
                active--;
                processedDeletedReferences.Add(b);
            }
            deletedReferences.Clear();
        }

        /// <summary>
        /// Overwrites the meaning of index <paramref name="overwritten"/> with
        /// <paramref name="reference"/> in the arrays.
        /// </summary>
        private void Overwrite(BulletReference reference, BulletReference overwritten) {
            int i = (int)reference;
            int j = (int)overwritten;
            Overwrite(i, j);
        }

        private void Overwrite(int i, int j) {
            Overwrite(i, j, x);
            Overwrite(i, j, y);
            Overwrite(i, j, z);
            Overwrite(i, j, dx);
            Overwrite(i, j, dy);
            Overwrite(i, j, textureID);
            Overwrite(i, j, layer);
            Overwrite(i, j, outerColor);
            Overwrite(i, j, innerColor);
        }

        private static void Overwrite<T>(int i, int j, NativeArray<T> arr) where T : struct {
            arr[j] = arr[i];
        }

        public void Dispose() {
            x.Dispose();
            y.Dispose();
            z.Dispose();
            dx.Dispose();
            dy.Dispose();
            textureID.Dispose();
            layer.Dispose();
            outerColor.Dispose();
            innerColor.Dispose();
        }
    }
}
