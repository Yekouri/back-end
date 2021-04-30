﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PolloPollo.Entities;

namespace PolloPollo.Entities.Migrations
{
    [DbContext(typeof(PolloPolloContext))]
    [Migration("20190325132718_Migration_V2")]
    partial class Migration_V2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.3-servicing-35854")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PolloPollo.Entities.Producer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("UserId");

                    b.Property<string>("Wallet");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Producers");
                });

            modelBuilder.Entity("PolloPollo.Entities.Receiver", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Receivers");
                });

            modelBuilder.Entity("PolloPollo.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("City");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("Description");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(191);

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("SurName")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("Thumbnail");

                    b.HasKey("Id");

                    b.HasAlternateKey("Email")
                        .HasName("AlternateKey_UserEmail");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PolloPollo.Entities.UserRole", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("UserRoleEnum");

                    b.HasKey("UserId", "UserRoleEnum");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("PolloPollo.Entities.Producer", b =>
                {
                    b.HasOne("PolloPollo.Entities.User", "User")
                        .WithOne("Producer")
                        .HasForeignKey("PolloPollo.Entities.Producer", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PolloPollo.Entities.Receiver", b =>
                {
                    b.HasOne("PolloPollo.Entities.User", "User")
                        .WithOne("Receiver")
                        .HasForeignKey("PolloPollo.Entities.Receiver", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PolloPollo.Entities.UserRole", b =>
                {
                    b.HasOne("PolloPollo.Entities.User")
                        .WithOne("UserRole")
                        .HasForeignKey("PolloPollo.Entities.UserRole", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
