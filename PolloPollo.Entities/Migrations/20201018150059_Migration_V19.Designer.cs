﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PolloPollo.Entities;

namespace PolloPollo.Entities.Migrations
{
    [DbContext(typeof(PolloPolloContext))]
    [Migration("20201018150059_Migration_V19")]
    partial class Migration_V19
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PolloPollo.Entities.Application", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<DateTime>("DateOfDonation");

                    b.Property<string>("DonationDate");

                    b.Property<DateTime>("LastModified");

                    b.Property<string>("Motivation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ProductId");

                    b.Property<int>("Status");

                    b.Property<string>("UnitId")
                        .HasMaxLength(44);

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("UserId");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("PolloPollo.Entities.ByteExchangeRate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("GBYTE_USD");

                    b.HasKey("Id");

                    b.ToTable("ByteExchangeRate");
                });

            modelBuilder.Entity("PolloPollo.Entities.Contracts", b =>
                {
                    b.Property<int>("ApplicationId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Bytes");

                    b.Property<int?>("Completed");

                    b.Property<string>("ConfirmKey");

                    b.Property<DateTime?>("CreationTime");

                    b.Property<string>("DonorDevice");

                    b.Property<string>("DonorWallet");

                    b.Property<int?>("Price");

                    b.Property<string>("ProducerDevice");

                    b.Property<string>("ProducerWallet");

                    b.Property<string>("SharedAddress");

                    b.HasKey("ApplicationId");

                    b.ToTable("Contracts");
                });

            modelBuilder.Entity("PolloPollo.Entities.Donor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AaAccount")
                        .IsRequired()
                        .HasMaxLength(128);

                    b.Property<string>("DeviceAddress")
                        .HasMaxLength(34);

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<string>("Password")
                        .HasMaxLength(64);

                    b.Property<string>("UID")
                        .HasMaxLength(32);

                    b.Property<string>("WalletAddress")
                        .IsRequired()
                        .HasMaxLength(34);

                    b.HasKey("Id");

                    b.ToTable("Donors");
                });

            modelBuilder.Entity("PolloPollo.Entities.Newsletter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DeviceAddress")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.HasIndex("DeviceAddress")
                        .IsUnique();

                    b.ToTable("Newsletter");
                });

            modelBuilder.Entity("PolloPollo.Entities.Producer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("DeviceAddress");

                    b.Property<string>("PairingSecret")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("Street")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("StreetNumber")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<int>("UserId");

                    b.Property<string>("WalletAddress");

                    b.Property<string>("Zipcode");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Producers");
                });

            modelBuilder.Entity("PolloPollo.Entities.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("Available");

                    b.Property<string>("Country")
                        .HasMaxLength(255);

                    b.Property<DateTime>("Created");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Location")
                        .HasMaxLength(255);

                    b.Property<int>("Price");

                    b.Property<int>("Rank");

                    b.Property<string>("Thumbnail");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Products");
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

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<DateTime>("Created");

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

            modelBuilder.Entity("PolloPollo.Entities.Application", b =>
                {
                    b.HasOne("PolloPollo.Entities.Product", "Product")
                        .WithMany("Applications")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PolloPollo.Entities.User", "User")
                        .WithMany("Applications")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PolloPollo.Entities.Producer", b =>
                {
                    b.HasOne("PolloPollo.Entities.User", "User")
                        .WithOne("Producer")
                        .HasForeignKey("PolloPollo.Entities.Producer", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PolloPollo.Entities.Product", b =>
                {
                    b.HasOne("PolloPollo.Entities.User", "User")
                        .WithMany("Products")
                        .HasForeignKey("UserId")
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
