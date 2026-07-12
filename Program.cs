using Microsoft.EntityFrameworkCore;
using HubClub.Data;

namespace HubClub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add MySQL DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            );

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            // Â–« «·ﬂÊœ ÌﬁÊ„ »≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰«  Ê«·Ãœ«Ê·  ·ﬁ«∆Ì« ⁄‰œ «·⁄„Ì·
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    context.Database.Migrate(); // Ì‰›– ﬂ· «·‹ Migrations
                }
                catch (Exception ex)
                {
                    //  ”ÃÌ· «·Œÿ√ ≈‰ ÊÃœ
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "ÕœÀ Œÿ√ √À‰«¡ ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰« .");
                }
            }

           
            app.Run();
        }
    }
}