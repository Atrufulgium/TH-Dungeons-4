using System.Collections.Generic;
using NUnit.Framework;

namespace Atrufulgium.EternalDreamCatcher.BulletField.Tests {
    public class FieldTests {

        [Test]
        public void TestDeletion() {
            Field f = new(true);
            List<BulletReference> bullets = new();
            BulletCreationParams bulletParams = default;
                
            for (int i = 0; i < 10; i++)
                bullets.Add(f.CreateBullet(ref bulletParams)!.Value);

            // This range forces the internal array to collapse.
            for (int i = 4; i < 8; i++)
                f.DeleteBullet(bullets[i]);
            f.FinalizeDeletion();

            // Now delete again, around what was previously deleted.
            // (Note that we have ""updated"" our references in the
            //  meantime: indices 8,9 are now represented by 5,4.)
            // This again forces array collapse.
            // This was what caused a bug.
            f.DeleteBullet(bullets[2]);
            f.DeleteBullet(bullets[4]);
            f.FinalizeDeletion();

            Assert.AreEqual(4, f.Active);
            f.Dispose();
        }
    }
}

