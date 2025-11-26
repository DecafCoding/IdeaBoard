using IdeaBoard.Components;
using IdeaBoard.Features.Authentication.Services;
using IdeaBoard.Features.Boards.Services;
using IdeaBoard.Services;
using IdeaBoard.Services.Interfaces;
using IdeaBoard.Shared.DataServices;
using IdeaBoard.Shared.Services;
using IdeaBoard.Shared.Services.Authentication;
using IdeaBoard.Shared.Services.Supabase;
using Microsoft.AspNetCore.Components.Authorization;

namespace IdeaBoard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            // Register theme service
            builder.Services.AddScoped<IThemeService, ThemeService>();

            // Register authentication services
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITokenStorage, CookieTokenStorage>();
            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<CustomAuthStateProvider>());
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<TokenRefreshService>();
            builder.Services.AddScoped<AuthSyncService>();
            builder.Services.AddAuthorizationCore();

            // Register Supabase HTTP Client with AuthHeaderHandler
            builder.Services.AddTransient<AuthHeaderHandler>();
            builder.Services.AddHttpClient<SupabaseHttpClient>()
                .AddHttpMessageHandler<AuthHeaderHandler>();
            // Note: AddHttpClient<T>() already registers T as transient
            // We use scoped for SupabaseService to match per-request auth token management

            // Register Supabase Service as Scoped (per-request auth token management)
            builder.Services.AddScoped<SupabaseService>();

            // Register other services (will be implemented in phases)
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<BoardService>();

            // Register data services
            builder.Services.AddScoped<DataEntityMapper>();
            builder.Services.AddScoped<BoardDataService>();
            builder.Services.AddScoped<BoardItemDataService>();

            // Register canvas services
            builder.Services.AddScoped<ConnectionStateService>();
            builder.Services.AddScoped<CanvasStateService>();
            builder.Services.AddScoped<CanvasInteropService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
