using Ae.Sample.Mcp.Data;
using Ae.Sample.Mcp.Dtos;
using Ae.Sample.Mcp.Services;
using Ae.Sample.Mcp.Settings;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Moq;
using System.Net.Http.Json;
using System.Net;

namespace Ae.Sample.Mcp.Tests.Services
{
    public class ClaimClientTests
    {
        private readonly Mock<ILogger<ClaimClient>> _mockLogger;
        private readonly Mock<IOptions<IdentityStorageApiOptions>> _mockApiOptions;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly IdentityStorageApiOptions _options;

        private const string BaseApiUrl = "http://localhost/api/";
        private const string ApiBasePath = "identity";
        private const string ClaimsEndpoint = "masterdata/claims";

        public ClaimClientTests()
        {
            _mockLogger = new Mock<ILogger<ClaimClient>>();
            _mockApiOptions = new Mock<IOptions<IdentityStorageApiOptions>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _options = new IdentityStorageApiOptions { ApiUrl = BaseApiUrl, ApiBasePath = ApiBasePath };
            _mockApiOptions.Setup(o => o.Value).Returns(_options);

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(_options.ApiUrl)
            };
        }

        private ClaimClient CreateClient()
        {
            return new ClaimClient(_mockLogger.Object, _mockApiOptions.Object, _httpClient, _mockMapper.Object);
        }

        // Renamed to clarify it produces the relative path part, and created a new helper for absolute URI
        private string ConstructExpectedAbsoluteUri(params string[] segments)
        {
            var relativePath = Flurl.Url.Combine(ApiBasePath, ClaimsEndpoint, Flurl.Url.Combine(segments));
            return new Uri(_httpClient.BaseAddress, relativePath).ToString();
        }

        private HttpResponseMessage CreateSuccessResponseMessage<T>(T content)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(content)
            };
        }

        private HttpResponseMessage CreateErrorResponseMessage(HttpStatusCode statusCode, string errorContent = "Error")
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorContent)
            };
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDependenciesAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ClaimClient(null, _mockApiOptions.Object, _httpClient, _mockMapper.Object));
            Assert.Throws<ArgumentNullException>(() => new ClaimClient(_mockLogger.Object, null, _httpClient, _mockMapper.Object));
            var nullOptions = new Mock<IOptions<IdentityStorageApiOptions>>();
            nullOptions.Setup(o => o.Value).Returns((IdentityStorageApiOptions)null);
            Assert.Throws<ArgumentNullException>(() => new ClaimClient(_mockLogger.Object, nullOptions.Object, _httpClient, _mockMapper.Object));
            Assert.Throws<ArgumentNullException>(() => new ClaimClient(_mockLogger.Object, _mockApiOptions.Object, null, _mockMapper.Object));
            Assert.Throws<ArgumentNullException>(() => new ClaimClient(_mockLogger.Object, _mockApiOptions.Object, _httpClient, null));
        }

        [Fact]
        public async Task LoadClaimsAsync_ReturnsMappedClaims_WhenApiCallIsSuccessful()
        {
            // Arrange
            var client = CreateClient();
            var claimDtos = new List<AppClaimDto> { new AppClaimDto { Id = Guid.NewGuid(), Type = "type1" } };
            var expectedClaims = new List<AppClaim> { new AppClaim { Id = claimDtos[0].Id, Type = "type1" } };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri()),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage(claimDtos));

            _mockMapper.Setup(m => m.Map<IEnumerable<AppClaim>>(claimDtos)).Returns(expectedClaims);

            // Act
            var result = await client.LoadClaimsAsync();

            // Assert
            Assert.Same(expectedClaims, result);
            _mockMapper.Verify(m => m.Map<IEnumerable<AppClaim>>(It.Is<IEnumerable<AppClaimDto>>(dtos => dtos.SequenceEqual(claimDtos))), Times.Once);
        }

        [Fact]
        public async Task LoadClaimsAsync_ReturnsEmptyList_WhenApiReturnsNull()
        {
            // Arrange
            var client = CreateClient();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri()),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage<IEnumerable<AppClaimDto>>(null));

            _mockMapper.Setup(m => m.Map<IEnumerable<AppClaim>>(null)).Returns([]);

            // Act
            var result = await client.LoadClaimsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task LoadClaimsAsync_ThrowsHttpRequestException_WhenApiCallFails()
        {
            // Arrange
            var client = CreateClient();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateErrorResponseMessage(HttpStatusCode.InternalServerError));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => client.LoadClaimsAsync());
        }

        [Fact]
        public async Task LoadClaimDetailsAsync_ReturnsMappedClaim_WhenApiCallIsSuccessful()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            var claimDto = new AppClaimDto { Id = Guid.Parse(claimId), Type = "type1" };
            var expectedClaim = new AppClaim { Id = claimDto.Id, Type = "type1" };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri(claimId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage(claimDto));
            _mockMapper.Setup(m => m.Map<AppClaim>(claimDto)).Returns(expectedClaim);

            // Act
            var result = await client.LoadClaimDetailsAsync(claimId);

            // Assert
            Assert.Same(expectedClaim, result);
            _mockMapper.Verify(m => m.Map<AppClaim>(It.Is<AppClaimDto>(dto => dto.Id == Guid.Parse(claimId))), Times.Once);
        }

        [Fact]
        public async Task LoadClaimDetailsAsync_ThrowsInvalidOperationException_WhenApiReturnsNullDto()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri(claimId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage<AppClaimDto>(null)); // Simulate API returning 200 OK with null body

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.LoadClaimDetailsAsync(claimId));
            Assert.Contains($"API returned null DTO for claim '{claimId}'", ex.Message);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsMappedClaim_WhenApiCallIsSuccessful()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            var claimDto = new AppClaimDto { Id = Guid.Parse(claimId), Type = "deletedType" };
            var expectedClaim = new AppClaim { Id = claimDto.Id, Type = "deletedType" };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri(claimId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage(claimDto));
            _mockMapper.Setup(m => m.Map<AppClaim>(claimDto)).Returns(expectedClaim);

            // Act
            var result = await client.DeleteClaimAsync(claimId);

            // Assert
            Assert.Same(expectedClaim, result);
        }

        [Fact]
        public async Task DeleteClaimAsync_ThrowsHttpRequestException_WhenApiReturnsError()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateErrorResponseMessage(HttpStatusCode.NotFound));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.DeleteClaimAsync(claimId));
            Assert.Contains($"Error deleting claim '{claimId}'", ex.Message);
            Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        }

        [Fact]
        public async Task CreateClaimAsync_ReturnsMappedClaim_WhenApiCallIsSuccessful()
        {
            // Arrange
            var client = CreateClient();
            var appClaimToCreate = new AppClaim { Type = "newType", Value = "newValue" };
            var requestDto = new AppClaimDto { Type = "newType", Value = "newValue" }; // Mapped from appClaimToCreate
            var responseDto = new AppClaimDto { Id = Guid.NewGuid(), Type = "newType", Value = "newValue" };
            var expectedCreatedClaim = new AppClaim { Id = responseDto.Id, Type = "newType", Value = "newValue" };

            _mockMapper.Setup(m => m.Map<AppClaimDto>(appClaimToCreate)).Returns(requestDto);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri()),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage(responseDto));
            _mockMapper.Setup(m => m.Map<AppClaim>(responseDto)).Returns(expectedCreatedClaim);

            // Act
            var result = await client.CreateClaimAsync(appClaimToCreate);

            // Assert
            Assert.Same(expectedCreatedClaim, result);
            _mockMapper.Verify(m => m.Map<AppClaimDto>(appClaimToCreate), Times.Once);
            _mockMapper.Verify(m => m.Map<AppClaim>(responseDto), Times.Once);
        }

        [Fact]
        public async Task CreateClaimAsync_ThrowsInvalidOperationException_WhenApiReturnsSuccessWithNullDto()
        {
            // Arrange
            var client = CreateClient();
            var appClaimToCreate = new AppClaim { Type = "newType", Value = "newValue" };
            var requestDto = new AppClaimDto { Type = "newType", Value = "newValue" };

            _mockMapper.Setup(m => m.Map<AppClaimDto>(appClaimToCreate)).Returns(requestDto);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri()),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage<AppClaimDto>(null)); // Success but null DTO

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateClaimAsync(appClaimToCreate));
            Assert.Contains("API returned success for creating new claim but the response content was null", ex.Message);
        }

        [Fact]
        public async Task UpdateClaimAsync_ReturnsMappedClaim_WhenApiCallIsSuccessful()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            var appClaimToUpdate = new AppClaim { Id = Guid.Parse(claimId), Type = "updatedType", Value = "updatedValue" };
            var requestDto = new AppClaimDto { Id = Guid.Parse(claimId), Type = "updatedType", Value = "updatedValue" };
            var responseDto = new AppClaimDto { Id = Guid.Parse(claimId), Type = "updatedType", Value = "updatedValue" };
            var expectedUpdatedClaim = new AppClaim { Id = responseDto.Id, Type = "updatedType", Value = "updatedValue" };

            _mockMapper.Setup(m => m.Map<AppClaimDto>(appClaimToUpdate)).Returns(requestDto);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Patch && req.RequestUri.ToString() == ConstructExpectedAbsoluteUri(claimId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateSuccessResponseMessage(responseDto));
            _mockMapper.Setup(m => m.Map<AppClaim>(responseDto)).Returns(expectedUpdatedClaim);

            // Act
            var result = await client.UpdateClaimAsync(claimId, appClaimToUpdate);

            // Assert
            Assert.Same(expectedUpdatedClaim, result);
        }

        [Fact]
        public async Task UpdateClaimAsync_ThrowsHttpRequestException_WhenApiReturnsError()
        {
            // Arrange
            var client = CreateClient();
            var claimId = Guid.NewGuid().ToString();
            var appClaimToUpdate = new AppClaim { Id = Guid.Parse(claimId), Type = "updatedType" };
            var requestDto = new AppClaimDto { Id = Guid.Parse(claimId), Type = "updatedType" };

            _mockMapper.Setup(m => m.Map<AppClaimDto>(appClaimToUpdate)).Returns(requestDto);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(CreateErrorResponseMessage(HttpStatusCode.BadRequest));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.UpdateClaimAsync(claimId, appClaimToUpdate));
            Assert.Contains($"Error updating claim '{claimId}'", ex.Message);
            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        }

        // Helper to verify log messages if needed, e.g., for specific error scenarios
        private void VerifyLog<TException>(LogLevel logLevel, string messageContains, Times times) where TException : Exception
        {
            _mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(messageContains)),
                    It.IsAny<TException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
    }
}
