using AutoMapper;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Ae.Sample.Mcp.Data;
using Ae.Sample.Mcp.Dtos;
using Ae.Sample.Mcp.Services;
using Ae.Sample.Mcp.Tools;

namespace Ae.Sample.Mcp.Tests.Tools
{
    public class ClaimToolsTests
    {
        private readonly Mock<IClaimClient> _mockClaimClient;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDtoValidator> _mockDtoValidator;
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

        public ClaimToolsTests()
        {
            _mockClaimClient = new Mock<IClaimClient>();
            _mockMapper = new Mock<IMapper>();
            _mockDtoValidator = new Mock<IDtoValidator>();
        }

        private T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);

        [Fact]
        public async Task GetClaimsAsync_ReturnsClaims_WhenClientHasData()
        {
            // Arrange
            var claims = new List<AppClaim> { new AppClaim { Id = Guid.NewGuid(), Type = "type1", Value = "value1" } };
            var claimDtos = new List<AppClaimOutgoingDto> { new AppClaimOutgoingDto { Id = claims[0].Id, Type = "type1", Value = "value1" } };
            _mockClaimClient.Setup(c => c.LoadClaimsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(claims);
            _mockMapper.Setup(m => m.Map<IEnumerable<AppClaimOutgoingDto>>(claims)).Returns(claimDtos);

            // Act
            var jsonResult = await ClaimTools.GetClaimsAsync(_mockClaimClient.Object, _mockMapper.Object);

            // Assert
            var resultDtos = Deserialize<List<AppClaimOutgoingDto>>(jsonResult);
            Assert.NotNull(resultDtos);
            Assert.Single(resultDtos); // Or Assert.Equal(1, resultDtos.Count);
            Assert.Equal(claimDtos[0].Id, resultDtos[0].Id);
            _mockClaimClient.Verify(c => c.LoadClaimsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<AppClaimOutgoingDto>>(claims), Times.Once);
        }

        [Fact]
        public async Task GetClaimsAsync_ReturnsEmptyList_WhenClientReturnsNull()
        {
            // Arrange
            _mockClaimClient.Setup(c => c.LoadClaimsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((List<AppClaim>)null);

            // Act
            var jsonResult = await ClaimTools.GetClaimsAsync(_mockClaimClient.Object, _mockMapper.Object);

            // Assert
            var resultDtos = Deserialize<List<AppClaimOutgoingDto>>(jsonResult);
            Assert.NotNull(resultDtos);
            Assert.Empty(resultDtos); // Or Assert.Equal(0, resultDtos.Count);
            _mockMapper.Verify(m => m.Map<IEnumerable<AppClaimOutgoingDto>>(It.IsAny<IEnumerable<AppClaim>>()), Times.Never);
        }

        [Fact]
        public async Task GetClaimDetailsAsync_ReturnsClaimDetails()
        {
            // Arrange
            var claimId = Guid.NewGuid().ToString();
            var claim = new AppClaim { Id = Guid.Parse(claimId), Type = "type1", Value = "value1" };
            var claimDto = new AppClaimOutgoingDto { Id = claim.Id, Type = "type1", Value = "value1" };
            _mockClaimClient.Setup(c => c.LoadClaimDetailsAsync(claimId, It.IsAny<CancellationToken>())).ReturnsAsync(claim);
            _mockMapper.Setup(m => m.Map<AppClaimOutgoingDto>(claim)).Returns(claimDto);

            // Act
            var jsonResult = await ClaimTools.GetClaimDetailsAsync(claimId, _mockClaimClient.Object, _mockMapper.Object);

            // Assert
            var resultDto = Deserialize<AppClaimOutgoingDto>(jsonResult);
            Assert.NotNull(resultDto);
            Assert.Equal(claimDto.Id, resultDto.Id);
            _mockClaimClient.Verify(c => c.LoadClaimDetailsAsync(claimId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsDeletedClaimDetails()
        {
            // Arrange
            var claimId = Guid.NewGuid().ToString();
            var deletedClaim = new AppClaim { Id = Guid.Parse(claimId), Type = "type1", Value = "value1" };
            var deletedClaimDto = new AppClaimOutgoingDto { Id = deletedClaim.Id, Type = "type1", Value = "value1" };
            _mockClaimClient.Setup(c => c.DeleteClaimAsync(claimId, It.IsAny<CancellationToken>())).ReturnsAsync(deletedClaim);
            _mockMapper.Setup(m => m.Map<AppClaimOutgoingDto>(deletedClaim)).Returns(deletedClaimDto);

            // Act
            var jsonResult = await ClaimTools.DeleteClaimAsync(claimId, _mockClaimClient.Object, _mockMapper.Object);

            // Assert
            var resultDto = Deserialize<AppClaimOutgoingDto>(jsonResult);
            Assert.NotNull(resultDto);
            Assert.Equal(deletedClaimDto.Id, resultDto.Id);
            _mockClaimClient.Verify(c => c.DeleteClaimAsync(claimId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateClaimAsync_ValidationFails_ReturnsError()
        {
            // Arrange
            var claimDto = new AppClaimCreateDto { Type = "", Value = "v" }; // Invalid DTO
            ICollection<ValidationResult> validationResults = [new ValidationResult("Type is required.")];
            _mockDtoValidator.Setup(v => v.TryValidate(claimDto, out validationResults)).Returns(false);

            // Act
            var jsonResult = await ClaimTools.CreateClaimAsync(claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var errorResponse = Deserialize<ErrorOutgoingDto>(jsonResult);
            Assert.NotNull(errorResponse?.Errors);
            Assert.Equal("Validation Failed", errorResponse.Status);
            Assert.Contains("Type is required.", errorResponse.Errors);
            _mockClaimClient.Verify(c => c.CreateClaimAsync(It.IsAny<AppClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateClaimAsync_ValidationSucceeds_CreatesAndReturnsClaim()
        {
            // Arrange
            var claimDto = new AppClaimCreateDto { Type = "type1", Value = "value1" };
            var appClaim = new AppClaim { Type = "type1", Value = "value1" }; // Id will be set by client
            var createdClaim = new AppClaim { Id = Guid.NewGuid(), Type = "type1", Value = "value1" };
            var createdClaimDto = new AppClaimOutgoingDto { Id = createdClaim.Id, Type = "type1", Value = "value1" };

            ICollection<ValidationResult> validationResults = null; // out param
            _mockDtoValidator.Setup(v => v.TryValidate(claimDto, out validationResults)).Returns(true);
            _mockMapper.Setup(m => m.Map<AppClaim>(claimDto)).Returns(appClaim);
            _mockClaimClient.Setup(c => c.CreateClaimAsync(appClaim, It.IsAny<CancellationToken>())).ReturnsAsync(createdClaim);
            _mockMapper.Setup(m => m.Map<AppClaimOutgoingDto>(createdClaim)).Returns(createdClaimDto);

            // Act
            var jsonResult = await ClaimTools.CreateClaimAsync(claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var resultDto = Deserialize<AppClaimOutgoingDto>(jsonResult);
            Assert.NotNull(resultDto);
            Assert.Equal(createdClaimDto.Id, resultDto.Id);
            _mockDtoValidator.Verify(v => v.TryValidate(claimDto, out validationResults), Times.Once);
            _mockMapper.Verify(m => m.Map<AppClaim>(claimDto), Times.Once);
            _mockClaimClient.Verify(c => c.CreateClaimAsync(appClaim, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(m => m.Map<AppClaimOutgoingDto>(createdClaim), Times.Once);
        }

        [Fact]
        public async Task UpdateClaimAsync_ClaimIdMismatch_ReturnsError()
        {
            // Arrange
            var pathClaimId = Guid.NewGuid().ToString();
            var claimDto = new AppClaimUpdateDto { Id = Guid.NewGuid(), Type = "type1", Value = "value1" }; // Different Id

            // Act
            var jsonResult = await ClaimTools.UpdateClaimAsync(pathClaimId, claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var errorResponse = Deserialize<ErrorOutgoingDto>(jsonResult);
            Assert.NotNull(errorResponse?.Errors);
            Assert.Equal("Validation Failed", errorResponse.Status);
            Assert.Contains("The claimId in the path must match the Id in the request body.", errorResponse.Errors);
            _mockDtoValidator.Verify(v => v.TryValidate(It.IsAny<AppClaimUpdateDto>(), out It.Ref<ICollection<ValidationResult>>.IsAny), Times.Never);
        }

        [Fact]
        public async Task UpdateClaimAsync_EmptyClaimIdInPath_ReturnsError()
        {
            // Arrange
            var claimDto = new AppClaimUpdateDto { Id = Guid.NewGuid(), Type = "type1", Value = "value1" };

            // Act
            var jsonResult = await ClaimTools.UpdateClaimAsync(" ", claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var errorResponse = Deserialize<ErrorOutgoingDto>(jsonResult);
            Assert.NotNull(errorResponse?.Errors);
            Assert.Equal("Validation Failed", errorResponse.Status);
            Assert.Contains("The claimId path parameter cannot be empty.", errorResponse.Errors);
        }


        [Fact]
        public async Task UpdateClaimAsync_ValidationFails_ReturnsError()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claimDto = new AppClaimUpdateDto { Id = claimId, Type = "", Value = "v" }; // Invalid DTO
            ICollection<ValidationResult> validationResults = [new ValidationResult("Type is required.")];
            _mockDtoValidator.Setup(v => v.TryValidate(claimDto, out validationResults)).Returns(false);

            // Act
            var jsonResult = await ClaimTools.UpdateClaimAsync(claimId.ToString(), claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var errorResponse = Deserialize<ErrorOutgoingDto>(jsonResult);
            Assert.NotNull(errorResponse?.Errors);
            Assert.Equal("Validation Failed", errorResponse.Status);
            Assert.Contains("Type is required.", errorResponse.Errors);
            _mockClaimClient.Verify(c => c.UpdateClaimAsync(It.IsAny<string>(), It.IsAny<AppClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateClaimAsync_ValidationSucceeds_UpdatesAndReturnsClaim()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claimDto = new AppClaimUpdateDto { Id = claimId, Type = "updatedType", Value = "updatedValue" };
            var appClaim = new AppClaim { Id = claimId, Type = "updatedType", Value = "updatedValue" };
            var updatedClaim = new AppClaim { Id = claimId, Type = "updatedType", Value = "updatedValue"};
            var updatedClaimDto = new AppClaimOutgoingDto { Id = claimId, Type = "updatedType", Value = "updatedValue" };

            ICollection<ValidationResult> validationResults = null; // out param
            _mockDtoValidator.Setup(v => v.TryValidate(claimDto, out validationResults)).Returns(true);
            _mockMapper.Setup(m => m.Map<AppClaim>(claimDto)).Returns(appClaim);
            _mockClaimClient.Setup(c => c.UpdateClaimAsync(claimId.ToString(), appClaim, It.IsAny<CancellationToken>())).ReturnsAsync(updatedClaim);
            _mockMapper.Setup(m => m.Map<AppClaimOutgoingDto>(updatedClaim)).Returns(updatedClaimDto);

            // Act
            var jsonResult = await ClaimTools.UpdateClaimAsync(claimId.ToString(), claimDto, _mockClaimClient.Object, _mockMapper.Object, _mockDtoValidator.Object);

            // Assert
            var resultDto = Deserialize<AppClaimOutgoingDto>(jsonResult);
            Assert.NotNull(resultDto);
            Assert.Equal(updatedClaimDto.Id, resultDto.Id);
            Assert.Equal(updatedClaimDto.Type, resultDto.Type);
            _mockDtoValidator.Verify(v => v.TryValidate(claimDto, out validationResults), Times.Once);
            _mockMapper.Verify(m => m.Map<AppClaim>(claimDto), Times.Once);
            _mockClaimClient.Verify(c => c.UpdateClaimAsync(claimId.ToString(), appClaim, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(m => m.Map<AppClaimOutgoingDto>(updatedClaim), Times.Once);
        }
    }

}
