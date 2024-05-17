using System.Collections.Generic;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    /// <summary>
    /// Represents a reference to a bullet.
    /// <br/>
    /// The lifetime of a reference is as follows:
    /// <list type="bullet">
    /// <item>
    /// First, you create a reference with <see cref="Field.CreateBullet(BulletCreationParams)"/>.
    /// </item>
    /// <item>
    /// After some time, you are done with this bullet. You delete it with
    /// <see cref="Field.DeleteBullet(BulletReference)"/>.
    /// <br/>
    /// After this, <b>the reference invalid but not yet collected</b>.
    /// </item>
    /// <item>
    /// Until the reference is collected, you can test this reference's
    /// invalidity with either <see cref="Field.FilterDeleted(IEnumerable{BulletReference})"/>
    /// or <see cref="Field.ReferenceIsDeleted(BulletReference)"/>.
    /// <br/>
    /// This is your <b>only</b> opportunity to properly handle disposal.
    /// </item>
    /// <item>
    /// After <see cref="Field.FinalizeDeletion"/>, the bullet is
    /// assumed (and not checked!) to be no longer referenced.
    /// This reference will then be reused for other bullets.
    /// </item>
    /// </list>
    /// If you encounter unexplainable bullet bugs, first check that your
    /// handling of deleted bullets is correct.
    /// </summary>
    // It's a bit hacky to use an empty enum for this, but I want some form of
    // type-safety and this is the easiest way.
    public enum BulletReference : ushort { }
}
