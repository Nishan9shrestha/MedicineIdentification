using Microsoft.EntityFrameworkCore;
using MedicineIdentification.Data;
using MedicineIdentification.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MedicineDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MedicineDbContext")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Medicine}/{action=Index}/{id?}");

app.Run();
