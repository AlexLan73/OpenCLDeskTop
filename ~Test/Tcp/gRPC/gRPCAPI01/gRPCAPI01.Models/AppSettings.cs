namespace gRPCAPI01.Models
{
    public class AppSettings
    {
        public bool EnableReflection { get; set; }
        public bool EnableJSONTranscoding { get; set; }
        public bool EnableSwagger { get; set; }
        public string SwaggerEndpoint { get; set; }
        public string SwaggerTitle { get; set; }
        public string Version { get; set; }
    }
}
