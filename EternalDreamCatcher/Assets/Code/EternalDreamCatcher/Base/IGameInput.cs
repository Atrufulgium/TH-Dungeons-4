using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// This class represents all input the player can do in-game.
    /// <br/>
    /// This should be implemented by structs, as that is required in all
    /// contexts that use this interface.
    /// </summary>
    public interface IGameInput {
        /// <summary>
        /// Some input systems rely on the current gametick.
        /// This function is called to message this.
        /// </summary>
        public int GameTick { set; }

        /// <summary>
        /// The <b>unnormalized</b> move direction of the player this tick.
        /// One of (0,0), (±1, 0), (0, ±1), or (±1,±1).
        /// </summary>
        public int2 MoveDirection { get; }

        /// <summary>
        /// Whether the player is firing any bullets this tick.
        /// </summary>
        public bool IsShooting { get; }

        /// <summary>
        /// Whether the player activated a bomb this tick. This should only be
        /// true on the down press, and not the ticks afterward.
        /// <br/>
        /// Any cooldown of the bomb is not the responsibility of this class.
        /// </summary>
        public bool IsBombing { get; }

        /// <summary>
        /// Whether the player is in focused movement this tick.
        /// </summary>
        public bool IsFocusing { get; }
    }

    /// <summary>
    /// Unity has annoying restrictions like "on the main thread" and stuff.
    /// <br/>
    /// If an input uses Unity methods, use this to prepare the members of
    /// <see cref="IGameInput"/>.
    /// </summary>
    public interface IUnityGameInput : IGameInput {
        /// <inheritdoc cref="IUnityGameInput"/>
        public void HandleInputMainThread();
    }
}
