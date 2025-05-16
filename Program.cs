namespace Build.AI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddHttpClient();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // CORS konfigürasyonu
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://127.0.0.1:5500") // veya "http://localhost:5500"
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // CORS middleware
            app.UseCors("AllowFrontend");

            // Statik dosyalar için middleware'ler (bunlarý ekle)
            app.UseDefaultFiles();  // index.html gibi dosyalarý otomatik bulur
            app.UseStaticFiles();   // wwwroot altýndaki statik dosyalarý sunar

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
