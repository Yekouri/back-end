﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PolloPollo.Entities;
using PolloPollo.Shared;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PolloPollo.Repository.Tests
{
    public class ProductRepositoryTests
    {
        [Fact]
        public async Task CreateAsync_given_null_returns_Null()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var result = await repository.CreateAsync(null);

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task CreateAsync_given_empty_DTO_returns_Null()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var productDTO = new ProductCreateDTO
                {
                    //Nothing
                };

                var result = await repository.CreateAsync(null);

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task CreateAsync_given_DTO_returns_DTO()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var productDTO = new ProductCreateDTO
                {
                    Title = "5 chickens",
                    UserId = 1,
                    Price = 42,
                    Description = "Test",
                    Location = "Test",
                };

                var result = await repository.CreateAsync(productDTO);

                Assert.Equal(productDTO.Title, result.Title);
                Assert.Equal(productDTO.UserId, result.UserId);
                Assert.Equal(productDTO.Price, result.Price);
                Assert.Equal(productDTO.Description, result.Description);
                Assert.Equal(productDTO.Location, result.Location);
            }
        }

        [Fact]
        public async Task CreateAsync_given_DTO_returns_DTO_with_id_1()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var productDTO = new ProductCreateDTO
                {
                    Title = "5 chickens",
                    UserId = 1,
                    Price = 42,
                    Description = "test",
                    Location = "tst",
                };

                var result = await repository.CreateAsync(productDTO);

                var expectedId = 1;

                Assert.Equal(expectedId, result.ProductId);
            }
        }

        [Fact]
        public async Task FindAsync_given_existing_Id_returns_ProductDTO()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {

                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var entity = new Product
                {
                    Title = "Chickens",
                    UserId = id,

                };

                context.Products.Add(entity);
                await context.SaveChangesAsync();

                var repository = new ProductRepository(context);

                var product = await repository.FindAsync(entity.Id);

                Assert.Equal(entity.Id, product.ProductId);
                Assert.Equal(entity.Title, product.Title);
            }
        }

        [Fact]
        public async Task FindAsync_given_nonExisting_Id_returns_null()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var result = await repository.FindAsync(1);

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task Read_returns_projection_of_all_products()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var product1 = new Product { Title = "Chickens", UserId = id, Available = true };
                var product2 = new Product { Title = "Eggs", UserId = id, Available = false};
                context.Products.AddRange(product1, product2);
                await context.SaveChangesAsync();

                var repository = new ProductRepository(context);

                var products = repository.Read();

                // There should only be one product in the returned list
                // since one of the created products is not available
                var count = products.ToList().Count;
                Assert.Equal(1, count);

                var product = products.First();

                Assert.Equal(1, product.ProductId);
                Assert.Equal(product1.Title, product.Title);
                Assert.Equal(product1.Available, product.Available);
            }
        }

        [Fact]
        public async Task Read_given_existing_id_returns_projection_of_all_products_by_specified_id()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                var otherId = 2; //
                
                var otherUser = new User
                {
                    Id = otherId,
                    Email = "other@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var otherUserEnumRole = new UserRole
                {
                    UserId = otherId,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var otherReceiver = new Receiver
                {
                    UserId = otherId,
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                context.Users.Add(otherUser);
                context.UserRoles.Add(otherUserEnumRole);
                context.Receivers.Add(otherReceiver);
                await context.SaveChangesAsync();

                var product1 = new Product { Title = "Chickens", UserId = id, Available = true };
                var product2 = new Product { Title = "Eggs", UserId = id, Available = false };
                var product3 = new Product { Title = "Chickens", UserId = otherId, Available = true };
                context.Products.AddRange(product1, product2, product3);
                await context.SaveChangesAsync();

                var repository = new ProductRepository(context);

                var products = repository.Read(1);

                // There should only be two products in the returned list
                // since one of the created products is by another producer
                var count = products.ToList().Count;
                Assert.Equal(2, count);

                var product = products.First();

                Assert.Equal(1, product.ProductId);
                Assert.Equal(product1.Title, product.Title);
                Assert.Equal(product1.Available, product.Available);
            }
        }

        [Fact]
        public async Task Read_given_nonExisting_id_returns_emptyCollection()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);
                var result = repository.Read(1);
                Assert.Empty(result);
            }
        }

        [Fact]
        public async Task UpdateAsync_with_existing_id_returns_True()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                var product = new Product
                {
                    Id = 1,
                    Title = "Eggs",
                    Available = false,
                    User = user,
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var expectedProduct = new ProductUpdateDTO
                {
                    Id = product.Id,
                    Available = false
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                var repository = new ProductRepository(context);

                var update = await repository.UpdateAsync(expectedProduct);

                Assert.True(update);
            }
        }

        [Fact]
        public async Task UpdateAsync_with_existing_id_updates_product()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@itu.dk",
                    Password = "1234",
                    FirstName = "test",
                    SurName = "test",
                    Country = "DK"
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var receiver = new Receiver
                {
                    UserId = id
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                await context.SaveChangesAsync();

                var product = new Product
                {
                    Id = 1,
                    Title = "Eggs",
                    Available = false,
                    UserId = id,
                };

                var expectedProduct = new ProductUpdateDTO
                {
                    Id = product.Id,
                    Available = true,
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                var repository = new ProductRepository(context);

                await repository.UpdateAsync(expectedProduct);

                var products = await context.Products.FindAsync(product.Id);

                Assert.Equal(expectedProduct.Id, products.Id);
                Assert.Equal(expectedProduct.Available, products.Available);
            }
        }

        [Fact]
        public async Task UpdateAsync_with_non_existing_id_returns_False()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var repository = new ProductRepository(context);

                var updateProductDTO = new ProductUpdateDTO
                {
                    Id = 42,
                    Available = false,
                };

                var result = await repository.UpdateAsync(updateProductDTO);

                Assert.False(result);
            }
        }

        //Below are internal methods for use during testing
        private async Task<DbConnection> CreateConnectionAsync()
        {
            var connection = new SqliteConnection("datasource=:memory:");
            await connection.OpenAsync();

            return connection;
        }

        private async Task<PolloPolloContext> CreateContextAsync(DbConnection connection)
        {
            var builder = new DbContextOptionsBuilder<PolloPolloContext>().UseSqlite(connection);

            var context = new PolloPolloContext(builder.Options);
            await context.Database.EnsureCreatedAsync();

            return context;
        }
    }
}