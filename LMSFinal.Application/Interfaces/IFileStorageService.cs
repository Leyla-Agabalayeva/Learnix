namespace LMSFinal.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(
            string bucket, string objectName, Stream content, string contentType,
            CancellationToken cancellationToken = default);

        Task<byte[]> DownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default);

        // null, если объекта нет — вызывающему коду так проще, чем ловить
        // специфичное для MinIO исключение "не найдено".
        Task<byte[]?> TryDownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default);

        Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken = default);

        Task DeleteByPrefixAsync(string bucket, string prefix, CancellationToken cancellationToken = default);

        string GetPublicUrl(string bucket, string objectName);
    }
}
