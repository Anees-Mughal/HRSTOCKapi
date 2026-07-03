using System.Text;
using HRStackPK.Api.DAL;
using HRStackPK.Api.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "HRStackPK API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste JWT access token"
    });
    o.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DAL + helpers
builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<AuthDAL>();
builder.Services.AddScoped<EmployeesDAL>();
builder.Services.AddScoped<DepartmentsDAL>();
builder.Services.AddScoped<AttendanceDAL>();
builder.Services.AddScoped<LeavesDAL>();
builder.Services.AddScoped<LoansDAL>();
builder.Services.AddScoped<PayrollDAL>();
builder.Services.AddScoped<PayslipsDAL>();
builder.Services.AddScoped<AccountsDAL>();
builder.Services.AddScoped<AppraisalsDAL>();

// JWT Bearer
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// CORS — web (Vite) + mobile (Metro / device)
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy("app", p =>
    p.WithOrigins(origins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .SetIsOriginAllowed(_ => true))); // relax for RN device testing; tighten in production

var app = builder.Build();

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("app");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
