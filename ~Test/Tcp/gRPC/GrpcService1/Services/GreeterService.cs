using Grpc.Core;
using GrpcService1;

namespace GrpcService1.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    public override async Task Chat(IAsyncStreamReader<HelloRequest> requestStream,
        IServerStreamWriter<HelloReply> responseStream,
        ServerCallContext context)
    {
        // Читаем запросы от клиента
        await foreach (var request in requestStream.ReadAllAsync())
        {
            Console.WriteLine($"Получено: {request.Name}");

            // Отправляем ответ
            await responseStream.WriteAsync(new HelloReply
            {
                Message = $"ECHO: {request.Name.ToUpper()}"
            });
        }
    }
}

