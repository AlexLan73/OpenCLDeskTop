using Grpc.Core;

using gRPCAPI01.Protos;

namespace gRPCAPI01.Services
{
    public class UserService : User.UserBase
    {
        private readonly ILogger<UserService> _logger;
        public UserService(ILogger<UserService> logger)
        {
            _logger = logger;
        }

        public override Task<UserReply> GetUser(UserRequest request, ServerCallContext context)
        {
            return Task.FromResult(new UserReply
            {
                Firstname = "John",
                Lastname = "Doe"
            });
        }
    }
}