using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using IdeaBoard.Shared.Services;
using IdeaBoard.Shared.DataServices;

namespace IdeaBoard.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Add HTTP client for Supabase
            builder.Services.AddHttpClient<SupabaseService>();

            // Register shared services
            builder.Services.AddScoped<SupabaseService>();
            builder.Services.AddScoped<NotificationService>();

            // Register data services
            builder.Services.AddScoped<DataEntityMapper>();
            builder.Services.AddScoped<BoardDataService>();
            builder.Services.AddScoped<BoardItemDataService>();

            // Register canvas services
            builder.Services.AddScoped<ConnectionStateService>();
            builder.Services.AddScoped<CanvasStateService>();
            builder.Services.AddScoped<CanvasInteropService>();

            await builder.Build().RunAsync();
        }
    }
}
