using gRPCAPI01.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gRPCAPI01.Tests.TestBaseTests
{
    [TestClass]
    public class IoCDITests : TestBase
    {
        [TestMethod]
        public async Task IoC_DI_ServiceProvider_OK()
        {
            Assert.IsNotNull(_serviceProvider);
        }


        [TestMethod]
        public async Task IoC_DI_LoggerFactory_OK()
        {
            var serviceProvider = _services.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            Assert.IsNotNull(loggerFactory);
        }

        [TestMethod]
        public async Task IoC_DI_IOptions_AppSettings_OK()
        {
            var serviceProvider = _services.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);

            var ioptions = serviceProvider.GetService<IOptions<AppSettings>>();
            Assert.IsNotNull(ioptions);
        }
    }
}
