using AutoMapper;
using LMSFinal.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMSFinal.Tests.Common
{

    public static class TestMapper
    {
        private static readonly Lazy<IMapper> Instance = new(Build);

        public static IMapper Create() => Instance.Value;

        private static IMapper Build()
        {
    
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);

            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }
    }
}
