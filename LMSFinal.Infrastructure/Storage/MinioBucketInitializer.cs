using LMSFinal.Application.Common;
using Minio;
using Minio.DataModel.Args;

namespace LMSFinal.Infrastructure.Storage
{
    // Создаёт бакеты при старте приложения, если их ещё нет — в проде их
    // обычно готовят заранее, но для docker-compose / первого запуска
    // удобнее не требовать ручной настройки через mc.
    public static class MinioBucketInitializer
    {
        public static async Task EnsureBucketsAsync(IMinioClient client, CancellationToken cancellationToken = default)
        {
            await EnsureBucketAsync(client, StorageBuckets.Avatars, publicRead: true, cancellationToken);
            await EnsureBucketAsync(client, StorageBuckets.LessonMaterials, publicRead: true, cancellationToken);
            await EnsureBucketAsync(client, StorageBuckets.CourseThumbnails, publicRead: true, cancellationToken);
            await EnsureBucketAsync(client, StorageBuckets.HeroSlides, publicRead: true, cancellationToken);
            await EnsureBucketAsync(client, StorageBuckets.Certificates, publicRead: false, cancellationToken);
        }

        private static async Task EnsureBucketAsync(
            IMinioClient client, string bucket, bool publicRead, CancellationToken cancellationToken)
        {
            var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken);
            if (!exists)
            {
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
            }

            if (publicRead)
            {
                var policy = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Effect": "Allow",
                            "Principal": { "AWS": ["*"] },
                            "Action": ["s3:GetObject"],
                            "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                        }
                    ]
                }
                """;

                await client.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy), cancellationToken);
            }
        }
    }
}
