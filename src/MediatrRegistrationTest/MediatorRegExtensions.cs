using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Threading.Tasks;
using System.Threading;
using FluentValidation;

namespace MediatrRegistrationTest
{
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
