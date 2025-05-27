using Scalar.AspNetCore;
using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Repository.Implementation;
using SplitWise.Service.Interface;
using SplitWise.Service.Implementation;
using Microsoft.EntityFrameworkCore;
using SplitWise.Service;
using SplitWise.Domain.Generic.Entity;
using SplitWise.Domain;
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(SplitWiseConstants.EMAIL_SETTINGS));

// Add services to the container.
builder.Services.AddAutoMapper(typeof(Program));

// Database Context
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped(typeof(IGroupMemberRepository), typeof(GroupMemberRepository));


// Services
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IGroupMemberService, GroupMemberService>();
builder.Services.AddScoped<IAppContextService, AppContextService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IActivityLoggerService, ActivityLoggerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
app.MapControllers();
app.UseStaticFiles();
app.Run();
