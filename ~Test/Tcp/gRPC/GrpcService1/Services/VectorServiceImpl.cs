using Grpc.Core;

namespace GrpcService1.Services;

class VectorServiceImpl : VectorService.VectorServiceBase
{
    public override async Task ProcessVector(
        IAsyncStreamReader<VectorRequest> requestStream,
        IServerStreamWriter<VectorResponse> responseStream,
        ServerCallContext context)
    {

        // 1. Сервер отправляет клиенту вектор [1, 2, 3, 4, 5]
        Console.WriteLine("Отправляю клиенту вектор...");
        await responseStream.WriteAsync(new VectorResponse
        {
            ModifiedNumbers = { 1, 2, 3, 4, 5 }  // Исходный вектор
        });

        // 2. Ожидаем модифицированный вектор от клиента
        await foreach (var request in requestStream.ReadAllAsync())
        {
            var modifiedVector = request.Numbers.ToList();
            Console.WriteLine($"Получен модифицированный вектор: [{string.Join(", ", modifiedVector)}]");
        }
    }
}

