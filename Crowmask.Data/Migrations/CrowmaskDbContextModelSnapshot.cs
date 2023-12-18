﻿// <auto-generated />
using System;
using Crowmask.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Crowmask.Data.Migrations
{
    [DbContext(typeof(CrowmaskDbContext))]
    partial class CrowmaskDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.25")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Crowmask.Data.Follower", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Inbox")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SharedInbox")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Followers");
                });

            modelBuilder.Entity("Crowmask.Data.OutboundActivity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Failures")
                        .HasColumnType("bigint");

                    b.Property<string>("Inbox")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JsonBody")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("PublishedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("Sent")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("OutboundActivities");
                });

            modelBuilder.Entity("Crowmask.Data.PrivateAnnouncement", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("AnnouncedObjectId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("PublishedAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("PrivateAnnouncements");
                });

            modelBuilder.Entity("Crowmask.Data.Submission", b =>
                {
                    b.Property<int>("SubmitId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CacheRefreshAttemptedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("CacheRefreshSucceededAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("FirstCachedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("FriendsOnly")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("PostedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("RatingId")
                        .HasColumnType("int");

                    b.Property<int>("SubtypeId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SubmitId");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("Crowmask.Data.SubmissionMedia", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<int?>("SubmissionSubmitId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SubmissionSubmitId");

                    b.ToTable("SubmissionMedia");
                });

            modelBuilder.Entity("Crowmask.Data.SubmissionTag", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<int?>("SubmissionSubmitId")
                        .HasColumnType("int");

                    b.Property<string>("Tag")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SubmissionSubmitId");

                    b.ToTable("SubmissionTag");
                });

            modelBuilder.Entity("Crowmask.Data.User", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int?>("Age")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CacheRefreshAttemptedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("CacheRefreshSucceededAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("FullName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Gender")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Location")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfileText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Crowmask.Data.UserAvatar", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<int?>("MediaId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserAvatar");
                });

            modelBuilder.Entity("Crowmask.Data.UserLink", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Site")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("UsernameOrUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserLink");
                });

            modelBuilder.Entity("Crowmask.Data.SubmissionMedia", b =>
                {
                    b.HasOne("Crowmask.Data.Submission", null)
                        .WithMany("Media")
                        .HasForeignKey("SubmissionSubmitId");
                });

            modelBuilder.Entity("Crowmask.Data.SubmissionTag", b =>
                {
                    b.HasOne("Crowmask.Data.Submission", null)
                        .WithMany("Tags")
                        .HasForeignKey("SubmissionSubmitId");
                });

            modelBuilder.Entity("Crowmask.Data.UserAvatar", b =>
                {
                    b.HasOne("Crowmask.Data.User", null)
                        .WithMany("Avatars")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Crowmask.Data.UserLink", b =>
                {
                    b.HasOne("Crowmask.Data.User", null)
                        .WithMany("Links")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Crowmask.Data.Submission", b =>
                {
                    b.Navigation("Media");

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("Crowmask.Data.User", b =>
                {
                    b.Navigation("Avatars");

                    b.Navigation("Links");
                });
#pragma warning restore 612, 618
        }
    }
}
