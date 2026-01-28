namespace Omni.Core.Web
{
    public sealed class CorsOptions
    {
        public string AllowOrigin { get; set; } = "*";
        public string AllowMethods { get; set; } = "GET, POST, OPTIONS";
        public string AllowHeaders { get; set; } = "Content-Type, Authorization";
        public string ExposeHeaders { get; set; } = string.Empty;
        public int MaxAge { get; set; } = 86400;
        public bool AllowCredentials { get; set; } = false;
    }
}