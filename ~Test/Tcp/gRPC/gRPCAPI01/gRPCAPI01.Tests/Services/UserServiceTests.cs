using Grpc.Net.Client;

using gRPCAPI01.Protos.Clients;


namespace gRPCAPI01.Tests.Services
{
    [TestClass]
    public class UserServiceTests : TestBase
    {
        [TestMethod]
        public async Task GetUser_OK()
        {
            try
            {
                using var channel = GrpcChannel.ForAddress("https://localhost:20010");
                var client = new User.UserClient(channel);
                var reply = await client.GetUserAsync(
                                  new UserRequest { Id = "1" });

                Assert.IsNotNull(reply);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}