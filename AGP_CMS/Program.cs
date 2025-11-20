using System;
using Abstractions;
using LegendaryChat;
using AGP_CMS.Core;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AGP_CMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddAGP_CMS();

            // Add additional services
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<AGP_CMS.Security.IRBACManager, AGP_CMS.Security.RBACManager>();

            // Add Web Market services
            builder.Services.AddSingleton<WebMarketService>();
            builder.Services.AddSingleton<FTPServerService>();

            // Add Chat module
            builder.Services.AddSingleton<IChatModule>(sp =>
            {
                var chatModule = new ChatModule();
                chatModule.Initialize(null);
                return chatModule;
            });

            var app = builder.Build();

            // Initialize SQLite database
            InitializeDatabase(app.Configuration);

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();

            // Serve user websites from wwwroot/user_websites
            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    System.IO.Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "user_websites")),
                RequestPath = "/user_websites"
            });

            app.UseRouting();
            app.UseAuthorization();

            // Use AGP_CMS middleware
            app.UseAGP_CMS();

            // Map Razor Pages
            app.MapRazorPages();

            // Map controllers for API endpoints
            app.MapControllers();

            // Add a simple health check endpoint
            app.MapGet("/api/health", () => new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                system = "AGP_CMS",
                version = "1.0.0"
            });

            // Chat System Endpoints
            var chatModule = app.Services.GetRequiredService<IChatModule>();

            // Get all chat rooms
            app.MapGet("/api/chat/rooms", async () =>
            {
                var rooms = await chatModule.GetRoomsAsync();
                return Results.Ok(new { success = true, data = rooms });
            });

            // Get a specific chat room
            app.MapGet("/api/chat/rooms/{roomId}", async (string roomId) =>
            {
                var room = await chatModule.GetRoomByIdAsync(roomId);
                if (room == null)
                    return Results.NotFound(new { success = false, message = "Room not found" });
                return Results.Ok(new { success = true, data = room });
            });

            // Create a new chat room
            app.MapPost("/api/chat/rooms", async (HttpContext context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<CreateRoomRequest>();
                if (request == null || string.IsNullOrEmpty(request.Name))
                    return Results.BadRequest(new { success = false, message = "Room name is required" });

                var result = await chatModule.CreateRoomAsync(request.Name, request.CreatedBy ?? "anonymous", request.IsPrivate);
                if (result.success)
                    return Results.Ok(new { success = true, message = result.message, roomId = result.roomId });
                return Results.BadRequest(new { success = false, message = result.message });
            });

            // Get messages in a room
            app.MapGet("/api/chat/rooms/{roomId}/messages", async (string roomId, int limit = 50) =>
            {
                var messages = await chatModule.GetMessagesAsync(roomId, limit);
                return Results.Ok(new { success = true, data = messages });
            });

            // Send a message to a room
            app.MapPost("/api/chat/rooms/{roomId}/messages", async (string roomId, HttpContext context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<SendMessageRequest>();
                if (request == null || string.IsNullOrEmpty(request.Content))
                    return Results.BadRequest(new { success = false, message = "Message content is required" });

                var result = await chatModule.SendMessageAsync(roomId, request.UserId ?? "anonymous", request.Username ?? "Anonymous", request.Content);
                if (result.success)
                    return Results.Ok(new { success = true, message = result.message, messageId = result.messageId });
                return Results.BadRequest(new { success = false, message = result.message });
            });

            // Delete a message
            app.MapDelete("/api/chat/messages/{messageId}", async (string messageId, string userId) =>
            {
                var success = await chatModule.DeleteMessageAsync(messageId, userId);
                if (success)
                    return Results.Ok(new { success = true, message = "Message deleted" });
                return Results.NotFound(new { success = false, message = "Message not found or unauthorized" });
            });

            // Get active users in a room
            app.MapGet("/api/chat/rooms/{roomId}/users", async (string roomId) =>
            {
                var users = await chatModule.GetActiveUsersAsync(roomId);
                return Results.Ok(new { success = true, data = users });
            });

            // Join a room
            app.MapPost("/api/chat/rooms/{roomId}/join", async (string roomId, HttpContext context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<JoinRoomRequest>();
                if (request == null || string.IsNullOrEmpty(request.UserId))
                    return Results.BadRequest(new { success = false, message = "UserId is required" });

                var success = await chatModule.JoinRoomAsync(roomId, request.UserId, request.Username ?? "Anonymous");
                if (success)
                    return Results.Ok(new { success = true, message = "Joined room successfully" });
                return Results.BadRequest(new { success = false, message = "Failed to join room" });
            });

            // Leave a room
            app.MapPost("/api/chat/rooms/{roomId}/leave", async (string roomId, string userId) =>
            {
                var success = await chatModule.LeaveRoomAsync(roomId, userId);
                if (success)
                    return Results.Ok(new { success = true, message = "Left room successfully" });
                return Results.BadRequest(new { success = false, message = "Failed to leave room" });
            });

            var urls = app.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://localhost:5000";
            Console.WriteLine($"AGP_CMS is starting...");
            Console.WriteLine($"Application URL: {urls}");
            Console.WriteLine($"Homepage: {urls}/");
            Console.WriteLine($"Blogs: {urls}/cms/blogs");
            Console.WriteLine($"Forums: {urls}/cms/forums");
            Console.WriteLine($"Health Check: {urls}/api/health");
            Console.WriteLine($"");
            Console.WriteLine($"Chat System:");
            Console.WriteLine($"  - List rooms: {urls}/api/chat/rooms");
            Console.WriteLine($"  - Create room: POST {urls}/api/chat/rooms");
            Console.WriteLine($"  - Send message: POST {urls}/api/chat/rooms/{{roomId}}/messages");
            Console.WriteLine($"");

            app.Run();
        }

        // Request models for API endpoints
        private record CreateRoomRequest(string Name, string? CreatedBy, bool IsPrivate = false);
        private record SendMessageRequest(string? UserId, string? Username, string Content);
        private record JoinRoomRequest(string UserId, string? Username);

        private static void InitializeDatabase(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";

                // Ensure the database file is created
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // Create basic tables for CMS functionality
                var createTablesCommand = connection.CreateCommand();
                createTablesCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'User',
                    Title TEXT,
                    Bio TEXT,
                    AvatarUrl TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    LastLoginAt TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS UserProfiles (
                    UserId INTEGER PRIMARY KEY,
                    DisplayName TEXT,
                    Location TEXT,
                    Website TEXT,
                    Twitter TEXT,
                    Github TEXT,
                    PostCount INTEGER NOT NULL DEFAULT 0,
                    LikesReceived INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Friends (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    FriendId INTEGER NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'pending',
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (FriendId) REFERENCES Users(Id),
                    UNIQUE(UserId, FriendId)
                );

                CREATE TABLE IF NOT EXISTS Sessions (
                    Id TEXT PRIMARY KEY,
                    UserId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    ExpiresAt TEXT NOT NULL,
                    IpAddress TEXT,
                    UserAgent TEXT,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS BlogPosts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Slug TEXT UNIQUE NOT NULL,
                    Content TEXT NOT NULL,
                    Excerpt TEXT,
                    AuthorId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    Published INTEGER NOT NULL DEFAULT 0,
                    ViewCount INTEGER NOT NULL DEFAULT 0,
                    CategoryId INTEGER,
                    FOREIGN KEY (AuthorId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS BlogComments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PostId INTEGER NOT NULL,
                    AuthorId INTEGER NOT NULL,
                    Content TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (PostId) REFERENCES BlogPosts(Id),
                    FOREIGN KEY (AuthorId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Downloads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    FilePath TEXT NOT NULL,
                    FileSize INTEGER NOT NULL,
                    CategoryId INTEGER,
                    UploadedBy INTEGER NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    DownloadCount INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (UploadedBy) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS ActivityLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ActivityType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                INSERT OR IGNORE INTO Settings (Key, Value, UpdatedAt)
                VALUES ('SystemInitialized', 'true', datetime('now'));
            ";

                createTablesCommand.ExecuteNonQuery();

                // Initialize forum tables using the DatabaseService
                var dbService = new DatabaseService(configuration);
                dbService.InitializeForumTables();
                dbService.InitializeDownloadTables();
                dbService.InitializeBlogCategoryTables();
                dbService.InitializeSettingsTables();

                // Initialize Web Market tables
                var webMarketService = new WebMarketService(configuration);
                webMarketService.InitializeWebMarketTables();

                Console.WriteLine("✓ Database initialized successfully");
                Console.WriteLine($"  Connection string: {connectionString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error initializing database: {ex.Message}");
                throw;
            }
        }
    }
}
