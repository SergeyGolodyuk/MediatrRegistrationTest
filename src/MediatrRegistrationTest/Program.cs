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
            // registration start
            var collection = new ServiceCollection();

            collection.AddScoped<IMediator, Mediator>();
            collection.AddTransient<ServiceFactory>(p => p.GetService);

            collection.ForScoped<SomeRequest, SomeResponse>()
                .WithValidation<SomeValidator>()
                .WithHandler<SomeHandler>();

            var provider = collection.BuildServiceProvider();

            // registration end

            // usage start

            var mediator = provider.GetService<IMediator>();

            var result1 = await mediator.Send(new SomeRequest());
            Console.WriteLine("Should be invalid and return null: " + (result1 == null));

            var result2 = await mediator.Send(new SomeRequest { In = "something" });
            Console.WriteLine("Should be valid and return result: " + result2.Out);

            // usage end
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
}
