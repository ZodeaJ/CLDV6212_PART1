using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Functions.Entities;
using Functions.Helpers;
using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions.Functions;

public class CustomersFunctions
{
    private readonly string _conn; // Storage connection
    private readonly string _table; // Table name

    public CustomersFunctions(IConfiguration cfg)
    {
        _conn = cfg["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
        _table = cfg["TABLE_CUSTOMER"] ?? "Customer";
    }

    // List all customers
    [Function("Customers_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<CustomerDto>();
        await foreach (var e in table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
            items.Add(Map.ToDto(e));

        return await HttpJson.Ok(req, items);
    }

    // Get single customer by ID
    [Function("Customers_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            var e = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            return await HttpJson.Ok(req, Map.ToDto(e.Value));
        }
        catch
        {
            return await HttpJson.NotFound(req, "Customer not found");
        }
    }

    public record CustomerCreateUpdate(string? Name, string? Surname, string? Username, string? Email, string? ShippingAddress);

    // Create a new customer
    [Function("Customers_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);

        if (input is null || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Email))
            return await HttpJson.Bad(req, "Name and Email are required");

        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var e = new CustomerEntity
        {
            Name = input.Name!,
            Surname = input.Surname ?? "",
            Username = input.Username ?? "",
            Email = input.Email!,
            ShippingAddress = input.ShippingAddress ?? ""
        };
        await table.AddEntityAsync(e);

        return await HttpJson.Created(req, Map.ToDto(e));
    }
}