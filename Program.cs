using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Contexto de Base de Datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // 1. CORRECCIÓN: La acción se llama 'Login', no 'Index'.
        // Además, usas [Route("login")] en el controlador, así que la ruta es "/login"
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.AccessDeniedPath = "/Login/Error"; // Asegúrate de tener una vista para esto o cámbialo a /login

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst("UsuarioId");

                if (userIdClaim == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var usuario = await dbContext.Usuarios.FindAsync(userId);

                    if (usuario == null || (usuario.estado != "Activo"))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            }
        };
    });

builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IEmailService, EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    // 2. CORRECCIÓN: Cambiar 'Index' por 'Login'
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();