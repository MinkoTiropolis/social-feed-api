using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialFeed.Data.Entities;

namespace SocialFeed.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        // Every query against Posts silently gets "AND DeletedAt IS NULL" added, so a soft
        // deleted post disappears from the feed and from every read endpoint without each
        // query having to remember. Restoring a post has to opt out with IgnoreQueryFilters().
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(280);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Mirrors the feed query exactly: skip deleted posts, newest first, with Id breaking
        // ties on equal timestamps. Because the index is already in that order, SQL Server can
        // seek straight to the cursor position and read forward instead of sorting the table
        // for every page.
        builder.HasIndex(p => new { p.DeletedAt, p.CreatedAt, p.Id })
            .IsDescending(false, true, true);

        // Deleting a user is not an operation this API exposes, and silently destroying their
        // posts would be the wrong default if it ever became one.
        builder.HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
