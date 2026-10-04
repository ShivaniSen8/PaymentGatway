using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Gateways;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    if (builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Database:UseInMemory"))
    {
        options.UseInMemoryDatabase("PaymentDb");
    }
    else
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("PaymentDb"));
    }
});

builder.Services.AddScoped<IPaymentService, PaymentApplicationService>();
builder.Services.AddHttpContextAccessor();
if (builder.Configuration.GetValue<bool>("PaymentGateway:UseMock"))
{
    builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
}
else
{
    builder.Services.AddHttpClient<IPaymentGateway, RazorpayPaymentGateway>(
        client => client.BaseAddress = new Uri("https://api.razorpay.com/"));
}
builder.Services.AddHttpClient("OrderService", client =>
{
    var orderServiceBaseUrl =
        builder.Configuration["OrderService:BaseUrl"];

    if (!Uri.TryCreate(
            orderServiceBaseUrl,
            UriKind.Absolute,
            out var baseAddress))
    {
        throw new InvalidOperationException(
            "OrderService:BaseUrl must be configured as an absolute URL.");
    }

    client.BaseAddress = baseAddress;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
