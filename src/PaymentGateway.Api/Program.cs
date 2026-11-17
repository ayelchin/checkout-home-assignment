using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Logging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayDiagnostics();
builder.Services.AddGatewayHttpApi();
builder.Services.AddMerchantAuthentication(builder.Configuration);
builder.Services.AddPaymentProcessing();
builder.Services.AddAcquiringBank(builder.Configuration);

WebApplication app = builder.Build();

app.UseMiddleware<RequestLoggingScopeMiddleware>();
app.UseHttpLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
