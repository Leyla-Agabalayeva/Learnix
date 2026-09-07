using LMSFinal.Application.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace LMSFinal.Infrastructure.Storage
{
    public class MinioFileStorageService : IFileStorageService
    {
        private readonly IMinioClient _client;
        private readonly StorageSettings _settings;

        public MinioFileStorageService(IMinioClient client, IOptions<StorageSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<string> UploadAsync(
            string bucket, string objectName, Stream content, string contentType,
            CancellationToken cancellationToken = default)
        {
            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType);

            await _client.PutObjectAsync(args, cancellationToken);

            return objectName;
        }

        public async Task<byte[]> DownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _client.GetObjectAsync(args, cancellationToken);

            return memoryStream.ToArray();
        }

        public async Task<byte[]?> TryDownloadAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await DownloadAsync(bucket, objectName, cancellationToken);
            }
            catch (ObjectNotFoundException)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            var args = new RemoveObjectArgs().WithBucket(bucket).WithObject(objectName);
            await _client.RemoveObjectAsync(args, cancellationToken);
        }

        public async Task DeleteByPrefixAsync(string bucket, string prefix, CancellationToken cancellationToken = default)
        {
            var listArgs = new ListObjectsArgs().WithBucket(bucket).WithPrefix(prefix).WithRecursive(true);

            await foreach (var item in _client.ListObjectsEnumAsync(listArgs, cancellationToken))
            {
                await DeleteAsync(bucket, item.Key, cancellationToken);
            }
        }

        public string GetPublicUrl(string bucket, string objectName) =>
            $"{_settings.PublicBaseUrl.TrimEnd('/')}/{bucket}/{objectName}";
    }
}
