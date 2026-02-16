using Microsoft.EntityFrameworkCore;
using CopilotStudioTestRunner.Domain.Entities;

namespace CopilotStudioTestRunner.Data;

public class TestRunnerDbContext : DbContext
{
    public TestRunnerDbContext(DbContextOptions<TestRunnerDbContext> options)
        : base(options)
    {
    }

    // Entities
    public DbSet<Document> Documents { get; set; }
    public DbSet<Chunk> Chunks { get; set; }
    public DbSet<TestSuite> TestSuites { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<Run> Runs { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<TranscriptMessage> TranscriptMessages { get; set; }
    public DbSet<JudgeSetting> JudgeSettings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Chunk>()
            .HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestCase>()
            .HasOne(tc => tc.Suite)
            .WithMany(ts => ts.TestCases)
            .HasForeignKey(tc => tc.SuiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestCase>()
            .HasOne(tc => tc.SourceDocument)
            .WithMany(d => d.GeneratedTestCases)
            .HasForeignKey(tc => tc.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Run>()
            .HasOne(r => r.Suite)
            .WithMany(ts => ts.Runs)
            .HasForeignKey(r => r.SuiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Run)
            .WithMany(run => run.Results)
            .HasForeignKey(r => r.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.TestCase)
            .WithMany(tc => tc.Results)
            .HasForeignKey(r => r.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TranscriptMessage>()
            .HasOne(tm => tm.Result)
            .WithMany(r => r.TranscriptMessages)
            .HasForeignKey(tm => tm.ResultId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure value conversions
        modelBuilder.Entity<Document>()
            .Property(d => d.Name)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<TestSuite>()
            .Property(ts => ts.Name)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<TestCase>()
            .Property(tc => tc.Name)
            .HasMaxLength(255)
            .IsRequired();

        // Create indexes for better query performance
        modelBuilder.Entity<Result>()
            .HasIndex(r => new { r.RunId, r.TestCaseId })
            .IsUnique();

        modelBuilder.Entity<Chunk>()
            .HasIndex(c => c.DocumentId);

        modelBuilder.Entity<TestCase>()
            .HasIndex(tc => tc.SuiteId);

        modelBuilder.Entity<Run>()
            .HasIndex(r => r.SuiteId);

        modelBuilder.Entity<TranscriptMessage>()
            .HasIndex(tm => tm.ResultId);
    }
}
