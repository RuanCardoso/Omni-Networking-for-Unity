namespace Omni.Shared
{
    public class Bridge
    {
        /// <summary>
        /// Indicates whether Dapper is used for database query result mapping.
        /// When set to true, Dapper provides efficient and direct SQL execution with object mapping,
        /// although it has partial compatibility with IL2CPP. Conversely, setting it to false
        /// utilizes Newtonsoft.Json for JSON-based object mapping, which supports IL2CPP and is more versatile for complex structures.
        /// The property default is set to true.
        /// </summary>
        public static bool UseDapper { get; set; } = true;
        public static bool EnableDeepDebug { get; set; } = true;
    }
}