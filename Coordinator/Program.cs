using Coordinator.Models.Contexts;
using Coordinator.Services;
using Coordinator.Services.Abstracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TwoPhaseCommitContext>(options =>
{
    options.UseSqlServer("Server=.;Database=coordinator;Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");
});



builder.Services.AddHttpClient("OrderAPI", c => c.BaseAddress = new Uri("https://localhost:7100"));
builder.Services.AddHttpClient("PaymentAPI", c => c.BaseAddress = new Uri("https://localhost:7146"));
builder.Services.AddHttpClient("StockAPI", c => c.BaseAddress = new Uri("https://localhost:7157"));


builder.Services.AddTransient<ITransactionService, TransactionService>();
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/create-order-transaction", async (ITransactionService transactionService) =>
{

    // phase 1 - prepare
    var transactionId = await transactionService.CreateTransactionAsync();
    await transactionService.PrepareServicesAsync(transactionId);
    bool transactionState = await transactionService.CheckReadyServicesAsync(transactionId);

    if (transactionState)
    {
        // phase 2 - commit
        await transactionService.CommitAsync(transactionId);
        transactionState = await transactionService.CheckTransactionStateServicesAsync(transactionId);
    }

    if (!transactionState) await transactionService.RollBackAsync(transactionId);

    return true;
});



app.Run();
