using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Ae.Sample.Mcp.Dtos;
using Ae.Sample.Mcp.Settings;
using Ae.Sample.Mcp.Tools;

namespace Ae.Sample.Mcp.Tests.Tools
{
    public class AppInfoToolTests
    {
        private readonly Mock<IOptions<AppOptions>> _mockAppOptions;

        public AppInfoToolTests()
        {
            _mockAppOptions = new Mock<IOptions<AppOptions>>();
        }

        [Fact]
        public void GetAppVersion_ReturnsCorrectJson()
        {
            // Arrange
            var appOptions = new AppOptions { Version = "1.2.3", Name = "TestApp" };
            _mockAppOptions.Setup(ap => ap.Value).Returns(appOptions);

            // Act
            var jsonResult = AppInfoTool.GetAppVersion(_mockAppOptions.Object);

            // Assert
            Assert.NotNull(jsonResult);
            var resultDto = JsonSerializer.Deserialize<AppVersionDto>(jsonResult);

            Assert.NotNull(resultDto);
            Assert.Equal(appOptions.Version, resultDto.AppVersion);
            Assert.True(resultDto.AppNow > DateTimeOffset.MinValue);
            Assert.True(resultDto.AppNowUtc > DateTimeOffset.MinValue);
            Assert.True(resultDto.AppUtcTicks > 0);
        }

        [Fact]
        public void GetAppVersion_HandlesNullOptions_ReturnsDefaultVersion()
        {
            _mockAppOptions.Setup(ap => ap.Value).Returns((AppOptions)null);
            var jsonResult = AppInfoTool.GetAppVersion(_mockAppOptions.Object);
            var resultDto = JsonSerializer.Deserialize<AppVersionDto>(jsonResult);
            Assert.Equal("?.?", resultDto.AppVersion);
        }
    }
}
