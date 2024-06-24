﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Atrufulgium.EternalDreamCatcher.BulletField.Tests {
    public class FieldTests {

        // easier notation
        IEnumerable<(BulletReference, BulletReference)> Moves(params (ushort, ushort)[] values)
            => values.Select((tuple) => ((BulletReference)tuple.Item1, (BulletReference)tuple.Item2));

        [Test]
        public void TestDeletion() {
            using Field f = new(true);

            List<BulletReference> bullets = new();
            BulletCreationParams bulletParams = default;
                
            for (int i = 0; i < 10; i++)
                bullets.Add(f.CreateBullet(ref bulletParams)!.Value);

            // This range forces the internal array to collapse as we create
            // a gap [....XXXX..].
            for (int i = 4; i < 8; i++)
                f.DeleteBullet(bullets[i]);
            f.FinalizeDeletion();
            Assert.AreEqual(6, f.Active);

            // NOTE: The array gets collapsed but order gets maintained, so
            // there is only one correct order for these.
            UnityEngine.Debug.Log("First removal moves:");
            foreach (var p in f.GetFinalizeDeletionShifts()) UnityEngine.Debug.Log(p);
            CollectionAssert.AreEqual(
                Moves((8, 4), (9, 5)),
                f.GetFinalizeDeletionShifts()
            );

            // Now delete again, around what was previously deleted.
            // This again forces array collapse.
            // This case caused a bug.
            // The collapse creates gaps [..X.X.], which can happen in 2 ways.
            f.DeleteBullet(bullets[2]);
            f.DeleteBullet(bullets[4]);
            f.FinalizeDeletion();
            Assert.AreEqual(4, f.Active);

            UnityEngine.Debug.Log("\nSecond removal moves:");
            foreach (var p in f.GetFinalizeDeletionShifts()) UnityEngine.Debug.Log(p);
            CollectionAssert.AreEqual(
                Moves((3,2), (5,3)),
                f.GetFinalizeDeletionShifts()
            );

            // Delete the rest [XXXX]
            for (int i = 0; i < 4; i++)
                f.DeleteBullet(bullets[i]);
            f.FinalizeDeletion();
            Assert.AreEqual(0, f.Active);
            Assert.AreEqual(0, f.GetFinalizeDeletionShifts().Count());
        }
    }
}