using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using IdeaBoard.Shared.Services;
using IdeaBoard.Shared.DataServices;
using IdeaBoard.Shared.Services.Authentication;
using IdeaBoard.Client.Services;
using IdeaBoard.Features.Authentication.Services;

namespace IdeaBoard.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Register authentication services
            builder.Services.AddScoped<ITokenStorage, LocalStorageTokenStorage>();
            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<CustomAuthStateProvider>());
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<TokenRefreshService>();
            builder.Services.AddScoped<AuthSyncService>();
            builder.Services.AddAuthorizationCore();

            // Add HTTP client for Supabase with AuthHeaderHandler
            builder.Services.AddTransient<AuthHeaderHandler>();
            builder.Services.AddHttpClient<SupabaseService>()
                .AddHttpMessageHandler<AuthHeaderHandler>();

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
