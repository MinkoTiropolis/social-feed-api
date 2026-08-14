using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialFeed.Data.Entities;

namespace SocialFeed.Data.Configurations;

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        // The pair is the identity. A second like by the same user on the same post is a
        // primary key violation rather than something the service layer has to check for.
        builder.HasKey(l => new { l.PostId, l.UserId });

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        // Hard deleting a post takes its likes with it, so no orphan rows survive the purge.
        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade. SQL Server rejects a schema with two cascade paths reaching
        // the same table, and this one would arrive both directly and via Posts.
        builder.HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
