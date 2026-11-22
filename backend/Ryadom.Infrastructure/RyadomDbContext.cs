using Microsoft.EntityFrameworkCore;

namespace Ryadom.Infrastructure;

public sealed class RyadomDbContext(DbContextOptions<RyadomDbContext> options) : DbContext(options)
{
    internal DbSet<AccountRow> Users => Set<AccountRow>();
    internal DbSet<AssessmentRow> Assessments => Set<AssessmentRow>();
    internal DbSet<SpecialistRow> Specialists => Set<SpecialistRow>();
    internal DbSet<SlotRow> Slots => Set<SlotRow>();
    internal DbSet<BookingRow> Bookings => Set<BookingRow>();
    internal DbSet<ResultRow> Results => Set<ResultRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var users = model.Entity<AccountRow>().ToTable("app_users");
        users.HasKey(x => x.Id);
        users.Property(x => x.Id).HasColumnName("id");
        users.Property(x => x.UserName).HasColumnName("username");
        users.Property(x => x.NormalizedName).HasColumnName("username_normalized");
        users.Property(x => x.PasswordHash).HasColumnName("password_hash");
        users.Property(x => x.Role).HasColumnName("role");
        users.Property(x => x.CreatedAt).HasColumnName("created_at");
        users.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("uq_app_users_normalized");

        var assessments = model.Entity<AssessmentRow>().ToTable("assessments");
        assessments.Property(x => x.Id).HasColumnName("id");
        assessments.Property(x => x.Title).HasColumnName("title");
        assessments.Property(x => x.Label).HasColumnName("label");
        assessments.Property(x => x.Description).HasColumnName("description");
        assessments.Property(x => x.Duration).HasColumnName("duration");
        assessments.Property(x => x.DisplayOrder).HasColumnName("display_order");

        var questions = model.Entity<QuestionRow>().ToTable("assessment_questions");
        questions.HasKey(x => new { x.AssessmentId, x.Position });
        questions.Property(x => x.AssessmentId).HasColumnName("assessment_id");
        questions.Property(x => x.Position).HasColumnName("position");
        questions.Property(x => x.Text).HasColumnName("question_text");
        questions.Property(x => x.Sensitive).HasColumnName("sensitive");
        assessments.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.AssessmentId);

        var options = model.Entity<OptionRow>().ToTable("assessment_options");
        options.HasKey(x => new { x.AssessmentId, x.Position });
        options.Property(x => x.AssessmentId).HasColumnName("assessment_id");
        options.Property(x => x.Position).HasColumnName("position");
        options.Property(x => x.Text).HasColumnName("option_text");
        assessments.HasMany(x => x.Options).WithOne().HasForeignKey(x => x.AssessmentId);

        var specialists = model.Entity<SpecialistRow>().ToTable("specialists");
        specialists.Property(x => x.Id).HasColumnName("id");
        specialists.Property(x => x.Name).HasColumnName("full_name");
        specialists.Property(x => x.Role).HasColumnName("professional_role");
        specialists.Property(x => x.ExperienceYears).HasColumnName("experience_years");
        specialists.Property(x => x.PriceRub).HasColumnName("price_rub");
        specialists.Property(x => x.Rating).HasColumnName("rating");
        specialists.Property(x => x.ImageUrl).HasColumnName("image_url");
        specialists.Property(x => x.DisplayOrder).HasColumnName("display_order");

        var topics = model.Entity<TopicRow>().ToTable("topics");
        topics.Property(x => x.Id).HasColumnName("id");
        topics.Property(x => x.Name).HasColumnName("name");

        specialists.HasMany(x => x.Topics).WithMany().UsingEntity<Dictionary<string, object>>(
            "specialist_topics",
            link => link.HasOne<TopicRow>().WithMany().HasForeignKey("topic_id"),
            link => link.HasOne<SpecialistRow>().WithMany().HasForeignKey("specialist_id"));

        var slots = model.Entity<SlotRow>().ToTable("consultation_slots");
        slots.Property(x => x.Id).HasColumnName("id");
        slots.Property(x => x.SpecialistId).HasColumnName("specialist_id");
        slots.Property(x => x.StartsAt).HasColumnName("starts_at");
        slots.HasOne<SpecialistRow>().WithMany().HasForeignKey(x => x.SpecialistId);

        var results = model.Entity<ResultRow>().ToTable("assessment_results");
        results.Property(x => x.Id).HasColumnName("id");
        results.Property(x => x.VisitorId).HasColumnName("visitor_id");
        results.Property(x => x.AssessmentId).HasColumnName("assessment_id");
        results.Property(x => x.Score).HasColumnName("score");
        results.Property(x => x.NeedsSupport).HasColumnName("needs_support");
        results.Property(x => x.CompletedAt).HasColumnName("completed_at");
        results.HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId);

        var bookings = model.Entity<BookingRow>().ToTable("bookings");
        bookings.Property(x => x.Id).HasColumnName("id");
        bookings.Property(x => x.VisitorId).HasColumnName("visitor_id");
        bookings.Property(x => x.SlotId).HasColumnName("slot_id");
        bookings.HasOne<SlotRow>().WithOne().HasForeignKey<BookingRow>(x => x.SlotId);
    }
}
