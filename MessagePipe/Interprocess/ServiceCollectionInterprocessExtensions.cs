using System;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;

using ReturnType = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace MessagePipe
{
    public static class ServiceCollectionInterprocessExtensions
    {
        public static ReturnType AddMessagePipeNamedPipeInterprocess(this IServiceCollection services, string pipeName)
        {
            return AddMessagePipeNamedPipeInterprocess(services, pipeName, _ => { });
        }

        public static ReturnType AddMessagePipeNamedPipeInterprocess(this IServiceCollection services, string pipeName, Action<MessagePipeInterprocessNamedPipeOptions> configure)
        {
            var options = new MessagePipeInterprocessNamedPipeOptions(pipeName);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(NamedPipeWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(NamedPipeDistributedPublisher<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(NamedPipeDistributedSubscriber<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(NamedPipeRemoteRequestHandler<,>), options.InstanceLifetime);
            return services;
        }

        static void Add(this IServiceCollection services, Type serviceType, InstanceLifetime scope)
        {
            services.Add(serviceType, serviceType, scope);
        }

        static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceLifetime scope)
        {
            var lifetime = (scope == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                : (scope == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;

            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
            services.Add(descriptor);
        }
    }
}
