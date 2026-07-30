using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Infrastructure.Persistence;

namespace SyrianStudyBot;

public static class StartupSeeder
{
    public static async Task SeedRoles(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in new[] { "Admin", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    public static async Task SeedAdminUser(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var adminEmail = config["Admin:Email"];
        var adminPassword = config["Admin:Password"];

        logger.LogInformation("Seeding admin user: {Email}", adminEmail);

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "AdminUser",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    TwoFactorEnabled = false,
                    PhoneNumber = "+963983050315"
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully.");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    logger.LogError("Failed to create admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// On startup, marks any documents stuck in <see cref="DocumentStatus.Processing"/>
    /// as <see cref="DocumentStatus.Failed"/> because the in-memory job queue was lost
    /// on restart. Documents uploaded close to a restart boundary can be re-uploaded.
    /// </summary>
    public static async Task ReconcileStaleDocuments(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var staleDocs = await db.Documents
            .Where(d => d.Status == DocumentStatus.Processing)
            .ToListAsync();

        foreach (var doc in staleDocs)
        {
            doc.Status = DocumentStatus.Failed;
            doc.StatusMessage = "Processing interrupted by server restart. Please re-upload.";
            doc.ProcessedAt = DateTime.UtcNow;
        }

        if (staleDocs.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogWarning("Marked {Count} stale processing documents as failed", staleDocs.Count);
        }
    }
}
