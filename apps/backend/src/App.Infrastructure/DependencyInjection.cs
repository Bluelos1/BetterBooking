using App.Application.Audit;
using App.Application.Common;
using App.Application.Listings;
using App.Application.Reservations;
using App.Application.Users;
using App.Infrastructure.Health;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Repositories;
using App.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure;

public static class DependencyInjection
{
    private const string ApplicationDatabaseConnectionName = "ApplicationDatabase";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddScoped<IAuditLog, NullAuditLog>();

        var connectionString = configuration.GetConnectionString(ApplicationDatabaseConnectionName);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, postgresOptions =>
                    postgresOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IAuditLog, EfAuditLog>();
            services.AddScoped<ResolveUserIdentityHandler>();
            services.AddScoped<GetListingHandler>();
            services.AddScoped<GetOwnerListingsHandler>();
            services.AddScoped<SearchListingsHandler>();
            services.AddScoped<CheckListingAvailabilityHandler>();
            services.AddScoped<CreateListingHandler>();
            services.AddScoped<PublishListingHandler>();
            services.AddScoped<ArchiveListingHandler>();
            services.AddScoped<UnpublishListingHandler>();
            services.AddScoped<GetGuestReservationsHandler>();
            services.AddScoped<CreateReservationHandler>();
            services.AddScoped<CancelReservationHandler>();
            services.AddScoped<ConfirmReservationPaymentHandler>();
            services.AddScoped<IApplicationUnitOfWork, EfApplicationUnitOfWork>();
            services.AddScoped<IListingRepository, EfListingRepository>();
            services.AddScoped<IReservationRepository, EfReservationRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();

            services.AddHealthChecks()
                .AddCheck<ApplicationDbContextHealthCheck>(
                    "application-database",
                    tags: ["ready", "database"]);
        }

        return services;
    }
}
