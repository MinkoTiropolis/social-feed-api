using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialFeed.Data.Entities;

namespace SocialFeed.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(280);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.DeletedAt, p.CreatedAt, p.Id })
            .IsDescending(false, true, true);

        builder.HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
