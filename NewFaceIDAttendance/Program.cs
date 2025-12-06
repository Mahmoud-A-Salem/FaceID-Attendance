using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

// إضافة Session
builder.Services.AddDistributedMemoryCache(); // لتخزين الـ Session بالذاكرة
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3); // مدة صلاحية الجلسة
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=home}/{action=index}/{id?}")
    .WithStaticAssets();

app.Run();
