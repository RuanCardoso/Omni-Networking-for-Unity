#if OMNI_VIRTUAL_PLAYER_ENABLED && UNITY_6000_3_OR_NEWER && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode;

namespace Omni.Core
{
    /// <summary>
    /// Represents the different types of virtual players that can be used in multiplayer testing.
    /// This enum is used to identify and manage different player instances in a multiplayer environment.
    /// </summary>
    public enum VirtualPlayer
    {
        /// <summary>
        /// The main player instance, typically representing the host or primary player.
        /// </summary>
        Main,
        /// <summary>
        /// The second player instance in a multiplayer session.
        /// </summary>
        Player2,
        /// <summary>
        /// The third player instance in a multiplayer session.
        /// </summary>
        Player3,
        /// <summary>
        /// The fourth player instance in a multiplayer session.
        /// </summary>
        Player4,
        /// <summary>
        /// Represents no specific player instance or an invalid player state.
        /// </summary>
        None,
    }

    /// <summary>
    /// Multiplayer PlayMode (MPPM) utility class that provides functionality for managing virtual players
    /// in Unity's multiplayer testing environment. This class is only available when OMNI_VIRTUAL_PLAYER_ENABLED
    /// is defined.
    /// </summary>
    public static class MPPM
    {
        /// <summary>
        /// Gets the current virtual player instance based on the player's tags.
        /// This property determines which virtual player is currently active in the multiplayer session.
        /// </summary>
        /// <returns>
        /// A VirtualPlayer enum value representing the current player instance.
        /// Returns VirtualPlayer.None if no valid player tag is found.
        /// </returns>
        public static VirtualPlayer Player
        {
            get
            {
                string tag = Tags.FirstOrDefault();
                return tag switch
                {
                    "Main" => VirtualPlayer.Main,
                    "Player2" => VirtualPlayer.Player2,
                    "Player3" => VirtualPlayer.Player3,
                    "Player4" => VirtualPlayer.Player4,
                    _ => VirtualPlayer.None
                };
            }
        }

        /// <summary>
        /// Gets the array of tags associated with the current player.
        /// These tags are used to identify and manage player instances in the multiplayer environment.
        /// </summary>
        /// <returns>
        /// An array of strings containing the player's tags.
        /// </returns>
        public static IReadOnlyList<string> Tags
        {
            get
            {
                return CurrentPlayer.Tags;
            }
        }

        /// <summary>
        /// Determines whether the current player is a virtual player (not the main player).
        /// This property is useful for distinguishing between the main player and additional virtual players
        /// in a multiplayer testing scenario.
        /// </summary>
        /// <returns>
        /// True if the current player is a virtual player (Player2, Player3, or Player4),
        /// false if it's the main player or no player is active.
        /// </returns>
        public static bool IsVirtualPlayer
        {
            get
            {
                return Player != VirtualPlayer.None && Player != VirtualPlayer.Main;
            }
        }
    }
}
#endif
