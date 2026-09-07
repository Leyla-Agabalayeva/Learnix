namespace LMSFinal.Infrastructure.Storage
{
    public class StorageSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool UseSsl { get; set; }

        // Базовый URL, по которому браузер достаёт публичные файлы (аватары,
        // материалы уроков) — отличается от Endpoint, если MinIO стоит за
        // прокси/другим портом наружу, чем внутри docker-сети.
        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}
