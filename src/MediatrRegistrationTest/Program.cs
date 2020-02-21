using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System;
using System.Threading.Tasks;
using System.Threading;
using FluentValidation;

namespace MediatrRegistrationTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var collection = new ServiceCollection();

            collection.AddScoped<IMediator, Mediator>();
            collection.AddTransient<ServiceFactory>(p => p.GetService);

            collection.ForScoped<SomeRequest, SomeResponse>()
                .WithValidation<SomeValidator>()
                .WithHandler<SomeHandler>();

            var provider = collection.BuildServiceProvider();

            var mediator = provider.GetService<IMediator>();

            var result1 = await mediator.Send(new SomeRequest());
            Console.WriteLine("Should be invalid and return null: " + (result1 == null));

            var result2 = await mediator.Send(new SomeRequest { In = "something" });
            Console.WriteLine("Should be valid and return result: " + result2.Out);
        }
    }

    public class SomeValidator : AbstractValidator<SomeRequest>
    {
        public SomeValidator()
        {
            RuleFor(c => c.In).NotEmpty();
        }
    }

    public class SomeHandler : IRequestHandler<SomeRequest, SomeResponse>
    {
        public Task<SomeResponse> Handle(SomeRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SomeResponse { Out = request.In + "+result" });
        }
    }

    public class SomeResponse
    {
        public string Out { get; set; }
    }

    public class SomeRequest : IRequest<SomeResponse>
    {
        public string In { get; set; }
    }

    public static class MediatorRegExtensions
    {
        public static ScopedRegistrator<TRequest, TResponse> ForScoped<TRequest, TResponse>(this IServiceCollection services) where TRequest : IRequest<TResponse>
        {
            return new ScopedRegistrator<TRequest, TResponse>(services);
        }

        public class ScopedRegistrator<TRequest, TResponse> where TRequest : IRequest<TResponse>
        {
            private readonly IServiceCollection _services;

            public ScopedRegistrator(IServiceCollection services)
            {
                _services = services;
            }

            public ScopedRegistrator<TRequest, TResponse> WithValidation<TValidator>() where TValidator : AbstractValidator<TRequest>
            {
                _services.AddScoped<TValidator>();
                _services.AddScoped<IPipelineBehavior<TRequest, TResponse>>(p => new ValidationBehavior<TRequest, TResponse>(p.GetService<TValidator>()));

                return this;
            }

            public ScopedRegistrator<TRequest, TResponse> WithHandler<THandler>() where THandler : class, IRequestHandler<TRequest, TResponse>
            {
                _services.AddScoped<IRequestHandler<TRequest, TResponse>, THandler>();

                return this;
            }
        }

        public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        {
            private readonly AbstractValidator<TRequest> _validator;

            public ValidationBehavior(AbstractValidator<TRequest> validator)
            {
                _validator = validator;
            }

            public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
            {
                var validationResult = await _validator
                    .ValidateAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                if (!validationResult.IsValid)
                {
                    return default(TResponse);
                }

                return await next();
            }
        }
    }
}
