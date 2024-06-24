using System.Collections.Generic;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Represents a reference to a bullet.
    /// <br/>
    /// The lifetime of a reference is as follows:
    /// <list type="bullet">
    /// <item>
    /// First, you create a reference with <see cref="BulletField.CreateBullet(ref BulletCreationParams)"/>.
    /// </item>
    /// <item>
    /// After some time, you are done with this bullet. You delete it with
    /// <see cref="BulletField.DeleteBullet(BulletReference)"/>.
    /// <br/>
    /// After this, <b>the reference invalid but not yet finalized</b>.
    /// </item>
    /// <item>
    /// Until the deletion is finalized, you can test this reference's
    /// invalidity with either <see cref="BulletField.FilterDeleted(IEnumerable{BulletReference})"/>
    /// or <see cref="BulletField.ReferenceIsDeleted(BulletReference)"/>.
    /// <br/>
    /// This is your <b>only</b> opportunity to properly handle disposal.
    /// </item>
    /// <item>
    /// After <see cref="BulletField.FinalizeDeletion"/>, the bullet is
    /// assumed (and not checked!) to be no longer referenced.
    /// This reference will then be reused for other bullets.
    /// <br/>
    /// This finalization moves around bullets in the array. This movement can
    /// be read from <see cref="BulletField.GetFinalizeDeletionShifts"/> or
    /// <see cref="BulletField.GetFinalizeDeletionShift(BulletReference)"/>.
    /// </item>
    /// </list>
    /// If you encounter unexplainable bullet bugs, first check that your
    /// handling of deleted bullets is correct.
    /// <br/>
    /// Do not use the underlying numeric type.
    /// </summary>
    // It's a bit hacky to use an empty enum for this, but I want some form of
    // type-safety but I'm lazy and `record struct` does not exist yet.
    // Code (other than Field) should never do any casting or arithmetic, and
    // just pass the reference around.
    public enum BulletReference : ushort { }
}
