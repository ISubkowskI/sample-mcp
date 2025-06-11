﻿using AutoMapper;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using Ae.Sample.Mcp.Dtos;
using Ae.Sample.Mcp.Services;
using Ae.Sample.Mcp.Data;

namespace Ae.Sample.Mcp.Tools
{
    /// <summary>
    /// Provides a collection of MCP server tools for managing and retrieving claims.
    /// This class contains static methods that expose claim-related functionality through the MCP server interface.
    /// </summary>
    /// <remarks>
    /// All methods in this class are exposed as MCP server tools and return JSON-serialized responses.
    /// The class uses AutoMapper for entity-to-DTO mapping and handles asynchronous claim operations.
    /// </remarks>
    [McpServerToolType]
    public static class ClaimTools
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

        /// <summary>
        /// Retrieves a list of claims from the claim client, maps them to DTOs, and serializes the result to JSON.
        /// </summary>
        /// <param name="claimClient">The client used to load claims.</param>
        /// <param name="mapper">The AutoMapper instance for mapping entities to DTOs.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A JSON string representing the list of claims.</returns>
        [McpServerTool(Name = "IdentityStorage.GetClaims"), Description("Get a list of claims.")]
        public static async Task<string> GetClaimsAsync(IClaimClient claimClient, IMapper mapper, CancellationToken ct = default)
        {
            var claims = await claimClient.LoadClaimsAsync(ct).ConfigureAwait(false);
            if (claims == null)
            {
                return JsonSerializer.Serialize(new List<AppClaimOutgoingDto>(), JsonSerializerOptions);
            }
            var res = mapper.Map<IEnumerable<AppClaimOutgoingDto>>(claims);
            return JsonSerializer.Serialize(res, JsonSerializerOptions);
        }

        /// <summary>
        /// Retrieves the details of a specific claim by its ID, maps it to a DTO, and serializes the result to JSON.
        /// </summary>
        /// <param name="claimId">The ID of the claim to retrieve.</param>
        /// <param name="claimClient">The client used to load claim details.</param>
        /// <param name="mapper">The AutoMapper instance for mapping entities to DTOs.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A JSON string representing the claim details.</returns>
        [McpServerTool(Name = "IdentityStorage.GetClaimDetails"), Description("Get a claim by id.")]
        public static async Task<string> GetClaimDetailsAsync(
            [Description("The id of the claim to get details for")] string claimId,
            IClaimClient claimClient,
            IMapper mapper,
            CancellationToken ct = default)
        {
            var claim = await claimClient.LoadClaimDetailsAsync(claimId, ct).ConfigureAwait(false);
            var res = mapper.Map<AppClaimOutgoingDto>(claim);
            return JsonSerializer.Serialize(res, JsonSerializerOptions);
        }

        /// <summary>
        /// Deletes a claim by its ID, maps the deleted claim to a DTO, and serializes the result to JSON.
        /// </summary>
        /// <param name="claimId">The ID of the claim to delete.</param>
        /// <param name="claimClient">The client used to delete the claim.</param>
        /// <param name="mapper">The AutoMapper instance for mapping entities to DTOs.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A JSON string representing the deleted claim details.</returns>
        [McpServerTool(Name = "IdentityStorage.DeleteClaim"), Description("Delete a claim by id.")]
        public static async Task<string> DeleteClaimAsync(
            [Description("The id of the claim to delete")] string claimId,
            IClaimClient claimClient,
            IMapper mapper,
            CancellationToken ct = default)
        {
            var deletedClaim = await claimClient.DeleteClaimAsync(claimId, ct).ConfigureAwait(false);
            var res = mapper.Map<AppClaimOutgoingDto>(deletedClaim);
            return JsonSerializer.Serialize(res, JsonSerializerOptions);
        }

        /// <summary>
        /// Creates a new claim using the provided DTO, maps it to the entity, and serializes the created claim to JSON.
        /// </summary>
        /// <param name="claimDto">The DTO representing the claim to create.</param>
        /// <param name="claimClient">The client used to create the claim.</param>
        /// <param name="mapper">The AutoMapper instance for mapping DTOs to entities and vice versa.</param>
        /// <param name="validator">The service used for validating the input DTO.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A JSON string representing the created claim details.</returns>
        [McpServerTool(Name = "IdentityStorage.CreateClaim"), Description("Create a new claim.")]
        public static async Task<string> CreateClaimAsync(
            [Description(@"The data for the new claim. The 'Id' will be generated by the server.
Expected JSON structure: 
{ 
  ""Type"": ""string"", 
  ""Value"": ""string"", 
  ""ValueType"": ""string"", 
  ""DisplayText"": ""string"", 
  ""Properties"": { ""key1"": ""value1"", ""key2"": ""value2"", ... } (optional dictionary of string key-value pairs), 
  ""Description"": ""string"" (optional, max 500 chars) 
}")] AppClaimCreateDto claimDto,
            IClaimClient claimClient,
            IMapper mapper,
            IDtoValidator validator,
            CancellationToken ct = default)
        {
            // Server-side validation using Data Annotations
            if (!validator.TryValidate(claimDto, out var validationResults))
            {
                // Validation failed, return error details
                var errors = validationResults.Select(r => r.ErrorMessage);
                return JsonSerializer.Serialize(new ErrorOutgoingDto { Errors = errors, Status = "Validation Failed" }, JsonSerializerOptions);
            }

            var appClaim = mapper.Map<AppClaim>(claimDto);
            var createdClaim = await claimClient.CreateClaimAsync(appClaim, ct).ConfigureAwait(false);
            var res = mapper.Map<AppClaimOutgoingDto>(createdClaim);
            return JsonSerializer.Serialize(res, JsonSerializerOptions);
        }

        /// <summary>
        /// Updates an existing claim by its ID using the provided DTO, maps it to the entity, and serializes the updated claim to JSON.
        /// </summary>
        /// <param name="claimId">The ID of the claim to update.</param>
        /// <param name="claimDto">The DTO representing the updated claim data.</param>
        /// <param name="claimClient">The client used to update the claim.</param>
        /// <param name="mapper">The AutoMapper instance for mapping DTOs to entities and vice versa.</param>
        /// <param name="validator">The service used for validating the input DTO.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A JSON string representing the updated claim details.</returns>
        [McpServerTool(Name = "IdentityStorage.UpdateClaim"), Description("Update a claim by id.")]
        public static async Task<string> UpdateClaimAsync(
            [Description("The id of the claim to update")] string claimId,
            [Description(@"The data to update the claim. The 'Id' in the body must match the 'claimId' in the path.
Expected JSON structure: 
{ 
  ""Id"": ""guid_string"",
  ""Type"": ""string"", 
  ""Value"": ""string"", 
  ""ValueType"": ""string"", 
  ""DisplayText"": ""string"", 
  ""Properties"": { ""key1"": ""value1"", ... } (optional dictionary of string key-value pairs), 
  ""Description"": ""string"" (optional, max 500 chars) 
}")] AppClaimUpdateDto claimDto,
            IClaimClient claimClient,
            IMapper mapper,
            IDtoValidator validator,
            CancellationToken ct = default)
        {
            // Validate claimId consistency and presence
            if (string.IsNullOrWhiteSpace(claimId))
            {
                return JsonSerializer.Serialize(new ErrorOutgoingDto { Errors = ["The claimId path parameter cannot be empty."],
                    Status = "Validation Failed" }, JsonSerializerOptions);
            }

            if (claimId != claimDto.Id.ToString())
            {
                return JsonSerializer.Serialize(new ErrorOutgoingDto { Errors = ["The claimId in the path must match the Id in the request body."],
                    Status = "Validation Failed" }, JsonSerializerOptions);
            }

            // Server-side validation using Data Annotations
            if (!validator.TryValidate(claimDto, out var validationResults))
            {
                // Validation failed, return error details
                var errors = validationResults.Select(r => r.ErrorMessage);
                return JsonSerializer.Serialize(new ErrorOutgoingDto { Errors = errors, Status = "Validation Failed" }, JsonSerializerOptions);
            }

            var appClaim = mapper.Map<AppClaim>(claimDto);
            var updatedClaim = await claimClient.UpdateClaimAsync(claimId, appClaim, ct).ConfigureAwait(false);
            var res = mapper.Map<AppClaimOutgoingDto>(updatedClaim);
            return JsonSerializer.Serialize(res, JsonSerializerOptions);
        }
    }
}
