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

            // CORS konfig�rasyonu
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

            // Statik dosyalar i�in middleware'ler (bunlar� ekle)
            app.UseDefaultFiles();  // index.html gibi dosyalar� otomatik bulur
            app.UseStaticFiles();   // wwwroot alt�ndaki statik dosyalar� sunar

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
