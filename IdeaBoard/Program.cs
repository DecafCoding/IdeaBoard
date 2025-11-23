using IdeaBoard.Components;
using IdeaBoard.Services;
using IdeaBoard.Services.Interfaces;
using IdeaBoard.Shared.DataServices;
using IdeaBoard.Shared.Services;

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

            // Add HTTP client for Supabase
            builder.Services.AddHttpClient<SupabaseService>();

            // Register services (will be implemented in phases)
            builder.Services.AddScoped<SupabaseService>();
            builder.Services.AddScoped<NotificationService>();
            // builder.Services.AddScoped<AuthService>();
            // builder.Services.AddScoped<BoardService>();

            // Register data services
            builder.Services.AddScoped<DataEntityMapper>();
            builder.Services.AddScoped<BoardDataService>();
            builder.Services.AddScoped<BoardItemDataService>();

            // Register canvas services
            builder.Services.AddScoped<ConnectionStateService>();
            builder.Services.AddScoped<CanvasStateService>();
            builder.Services.AddScoped<CanvasInteropService>();

            // Configure authentication (will be implemented in Phase 1)
            // builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            // builder.Services.AddAuthorizationCore();

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
