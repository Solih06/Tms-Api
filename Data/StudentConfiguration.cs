using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RegistrationNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
    // 1. Configure a Shadow Property for auditing (exists in DB schema, but NOT on the Student class)
        builder.Property<DateTime>("LastUpdated");

        // 2. Configure the Concurrency Token (Npgsql maps IsRowVersion to PostgreSQL's system xmin column)
        builder.Property(s => s.Version).IsRowVersion();
    }
}
    

