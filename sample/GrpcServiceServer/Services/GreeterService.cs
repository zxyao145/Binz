using Binz.Server;
using Grpc.Core;
using Proto.GreeterService;

namespace GrpcServiceServer.Services
{

    [BinzService]
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly HelloService helloService;

        public GreeterService(ILogger<GreeterService> logger,
            HelloService helloService)
        {
            _logger = logger;
            this.helloService = helloService;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("GreeterService receive HelloReques:{0}", request);
            return Task.FromResult(new HelloReply
            {
                Message = helloService.SayHello(request.Name + ", from GreeterService")
            });
        }
    }
}