﻿using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PolloPollo.Entities;
using PolloPollo.Repository.Utils;
using PolloPollo.Shared;
using PolloPollo.Shared.DTO;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace PolloPollo.Repository.Tests
{
    public class UserRepositoryTests
    {
        private readonly IPolloPolloContext _context;
        private readonly IUserRepository _repository;

        public UserRepositoryTests()
        {
             //Connection
            var connection = new SqliteConnection("datasource=:memory:");
            connection.Open();

            //Context
            var builder = new DbContextOptionsBuilder<PolloPolloContext>().UseSqlite(connection);
            var context = new PolloPolloTestContext(builder.Options);
            context.Database.EnsureCreated();
            _context = context;

            //MockImageWriter
            var imageWriter = new Mock<IImageWriter>();

            //Repository
            _repository = new UserRepository(GetSecurityConfig(), imageWriter.Object, _context);
        }

        [Fact]
        public async Task Authenticate_given_valid_Password_returns_Token()
        {
            var token = await _repository.Authenticate("receiver@test.com", "12345678");

            Assert.NotNull(token);
        }

        [Fact]
        public async Task Authenticate_given_non_existing_user_returns_Null()
        {
            var (status, userDTO, token) = await _repository.Authenticate("Not@A.User", "verysecret123");

            Assert.Equal(UserAuthStatus.NO_USER, status);
            Assert.Null(token);
            Assert.Null(userDTO);
        }

        [Fact]
        public async Task Authenticate_given_valid_Password_with_Receiver_returns_DetailedReceiverDTO()
        {
            var (status, dto, token) = await _repository.Authenticate("receiver@test.com", "12345678");

            var detailReceiver = dto as DetailedReceiverDTO;

            Assert.Equal(-1, detailReceiver.UserId);
            Assert.Equal("receiver@test.com", detailReceiver.Email);
            Assert.Equal(UserRoleEnum.Receiver.ToString(), detailReceiver.UserRole);
            Assert.NotNull(token);
        }

        [Fact]
        public async Task Authenticate_given_valid_Password_with_Producer_returns_DetailedProducerDTO()
        {
            var (status, dto, token) = await _repository.Authenticate("producer@test.com", "12345678");

            var detailProducer = dto as DetailedProducerDTO;

            Assert.Equal(-2, detailProducer.UserId);
            Assert.Equal("producer@test.com", detailProducer.Email);
            Assert.Equal(UserRoleEnum.Producer.ToString(), detailProducer.UserRole);
            Assert.Equal("test", detailProducer.Wallet);
            Assert.Equal(ConstructPairingLink("secret"), detailProducer.PairingLink);
            Assert.NotNull(token);
        }

        [Fact]
        public async Task Authenticate_given_invalid_Password_returns_Null()
        {
            var (status, id, token) = await _repository.Authenticate("receiver@test.com", "wrongpassword");
            Assert.Null(token);
        }

        [Fact]
        public async Task CreateAsync_given_User_invalid_role_returns_INVALID_ROLE()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "Test@Test",
                Country = "CountryCode",
                UserRole = "test",
                Password = "12345678"
            };

            var (status, tokenDTO) = await _repository.CreateAsync(dto);

            Assert.Equal(UserCreateStatus.INVALID_ROLE, status);
        }

        [Fact]
        public async Task CreateAsync_creates_User_with_timestamp()
        {
            var dto = new ReceiverCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "Test@Test",
                Country = "CountryCode",
                UserRole = UserRoleEnum.Receiver.ToString(),
                Password = "12345678"
            };

            var userId = 1;

            await _repository.CreateAsync(dto);

            var user = _context.Users.Find(userId);

            var now = DateTime.UtcNow;
            // These checks are to assume the timestamp is set on update.
            // The now timestamp is some ticks off from the database timestamp.
            Assert.Equal(now.Date, user.Created.Date);
            Assert.Equal(now.Hour, user.Created.Hour);
            Assert.Equal(now.Minute, user.Created.Minute);
        }

        [Fact]
        public async Task CreateAsync_given_role_Receiver_creates_Receiver_and_returns_TokenDTO()
        {
            var dto = new ReceiverCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "Test@Test",
                Country = "CountryCode",
                UserRole = UserRoleEnum.Receiver.ToString(),
                Password = "12345678"
            };

            var expectedDTO = new TokenDTO
            {
                UserDTO = new DetailedUserDTO
                {
                    UserId = 1,
                    UserRole = UserRoleEnum.Receiver.ToString(),
                    Email = dto.Email
                }
            };

            var (status, tokenDTO) = await _repository.CreateAsync(dto);

            Assert.Equal(UserCreateStatus.SUCCESS, status);
            Assert.Equal(expectedDTO.UserDTO.UserId, tokenDTO.UserDTO.UserId);
            Assert.Equal(expectedDTO.UserDTO.UserRole, tokenDTO.UserDTO.UserRole);
            Assert.Equal(expectedDTO.UserDTO.Email, tokenDTO.UserDTO.Email);
        }

        [Fact]
        public async Task CreateAsync_given_role_Producer_creates_Producer_and_returns_TokenDTO()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "Test@Test",
                Country = "CountryCode",
                UserRole = UserRoleEnum.Producer.ToString(),
                Password = "12345678",
                Street = "Test",
                StreetNumber = "Some number",
                Zipcode = "1234",
                City = "City"
            };

            var expectedDTO = new TokenDTO
            {
                UserDTO = new DetailedUserDTO
                {
                    UserId = 1,
                    UserRole = UserRoleEnum.Producer.ToString(),
                    Email = dto.Email,
                }
            };

            var (status, tokenDTO) = await _repository.CreateAsync(dto);

            var producer = await _context.Producers.FindAsync(tokenDTO.UserDTO.UserId);

            var detailedProducer = tokenDTO.UserDTO as DetailedProducerDTO;

            Assert.Equal(UserCreateStatus.SUCCESS, status);
            Assert.Equal(expectedDTO.UserDTO.UserId, tokenDTO.UserDTO.UserId);
            Assert.Equal(expectedDTO.UserDTO.UserRole, tokenDTO.UserDTO.UserRole);
            Assert.Equal(expectedDTO.UserDTO.Email, tokenDTO.UserDTO.Email);
            Assert.NotNull(producer.PairingSecret);
            Assert.Equal(ConstructPairingLink(producer.PairingSecret), detailedProducer.PairingLink);
            Assert.Equal(dto.Zipcode, detailedProducer.Zipcode);
        }

        [Fact]
        public async Task CreateAsync_given_empty_DTO_returns_Null()
        {
            var dto = new UserCreateDTO();

            var (status, tokenDTO) = await _repository.CreateAsync(dto);

            Assert.Null(tokenDTO);
        }

        [Fact]
        public async Task CreateAsync_given_Null_returns_NULL_INPUT()
        {
            var (status, tokenDTO) = await _repository.CreateAsync(default(UserCreateDTO));

            Assert.Equal(UserCreateStatus.NULL_INPUT, status);
        }

        [Fact]
        public async Task CreateAsync_given_no_password_returns_MISSING_PASSWORD()
        {
            var userCreateDTO = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Johnson",
                Email = "Test@Johnson.com",
                Password = ""
            };

            var (status, tokenDTO) = await _repository.CreateAsync(userCreateDTO);

            Assert.Equal(UserCreateStatus.MISSING_PASSWORD, status);
        }

        [Fact]
        public async Task CreateAsync_given_Password_under_8_length_returns_PASSWORD_TOO_SHORT()
        {
            var userCreateDTO = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Johnson",
                Email = "Test@Johnson.com",
                Password = "1234"
            };

            var (status, tokenDTO) = await _repository.CreateAsync(userCreateDTO);

            Assert.Equal(UserCreateStatus.PASSWORD_TOO_SHORT, status);
        }

        [Fact]
        public async Task CreateAsync_given_existing_user_returns_Null()
        {
            var userCreateDTO = new UserCreateDTO
            {
                Email = "receiver@test",
                Password = "87654321"
            };

            var (status, tokenDTO) = await _repository.CreateAsync(userCreateDTO);

            Assert.Null(tokenDTO);
        }

        [Fact]
        public async Task FindAsync_given_existing_id_returns_User()
        {
            var userDTO = await _repository.FindAsync(-1);

            Assert.Equal(-1, userDTO.UserId);
            Assert.Equal("receiver@test.com", userDTO.Email);
        }

        [Fact]
        public async Task FindAsync_given_existing_id_returns_Producer_With_PairingSecret()
        {
            var userDTO = await _repository.FindAsync(-2);
            var newDTO = userDTO as DetailedProducerDTO;

            Assert.Equal(-2, userDTO.UserId);
            Assert.Equal("producer@test.com", userDTO.Email);
            Assert.Equal(ConstructPairingLink("secret"), newDTO.PairingLink);
            Assert.Equal("1234", newDTO.Zipcode);
        }

        [Fact]
        public async Task FindAsync_given_existing_id_returns_Producer_Without_PairingLink()
        {
                var userDTO = await _repository.FindAsync(-3);
                var newDTO = userDTO as DetailedProducerDTO;

                Assert.Equal(-3, userDTO.UserId);
                Assert.Equal("producer1@test.com", userDTO.Email);
                Assert.Equal(default(string), newDTO.PairingLink);
        }
/* DeadTest
        [Fact]
        public async Task FindAsync_given_existing_id_for_User_with_invalid_Role_returns_Null()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = "1234",
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var expected = new DetailedUserDTO
            {
                UserId = 1,
                Email = user.Email
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDTO = await _repository.FindAsync(-4);

            Assert.Null(userDTO);
        }
*/
        [Fact]
        public async Task FindAsync_given_existing_id_for_Receiver_returns_Receiver()
        {
            var userDTO = await _repository.FindAsync(-1);

            Assert.Equal(-1, userDTO.UserId);
            Assert.Equal("receiver@test.com", userDTO.Email);
            Assert.Equal(UserRoleEnum.Receiver.ToString(), userDTO.UserRole);
        }

        [Fact]
        public async Task FindAsync_given_existing_id_for_Producer_returns_Producer()
        {
            var userDTO = await _repository.FindAsync(-2);
            var newDTO = userDTO as DetailedProducerDTO;

            Assert.Equal(-2, userDTO.UserId);
            Assert.Equal("producer@test.com", userDTO.Email);
            Assert.Equal(UserRoleEnum.Producer.ToString(), userDTO.UserRole);
            Assert.Equal(ConstructPairingLink("secret"), newDTO.PairingLink);
            Assert.Equal("teststreet", newDTO.Street);
            Assert.Equal("testnumber", newDTO.StreetNumber);
            Assert.Equal("testcity", newDTO.City);
        }

        [Fact]//Old style
        public async Task FindAsync_given_existing_id_for_Producer_returns_Producer_With_Stats()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = "1234",
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Producer
            };

            var producer = new Producer
            {
                UserId = id,
                PairingSecret = "ABCD",
                Street = "Test",
                StreetNumber = "Some number",
                City = "City"
            };

            var product = new Product
            {
                Id = id,
                UserId = user.Id,
                Price = 5,
                Title = "TEST",
                Available = true,
                Created = DateTime.UtcNow
            };

            var application = new Application
            {
                UserId = user.Id,
                ProductId = product.Id,
                Motivation = "Test",
                Status = ApplicationStatusEnum.Completed,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var application2 = new Application
            {
                UserId = user.Id,
                ProductId = product.Id,
                Motivation = "Test",
                Status = ApplicationStatusEnum.Completed,
                Created = DateTime.UtcNow - new TimeSpan(10, 0, 0, 0),
                LastModified = DateTime.UtcNow - new TimeSpan(10, 0, 0, 0)
            };

            var expected = new DetailedProducerDTO
            {
                UserId = id,
                Email = user.Email,
                UserRole = userEnumRole.UserRoleEnum.ToString(),
                PairingLink = ConstructPairingLink(producer.PairingSecret),
                Street = "Test",
                StreetNumber = "Some number",
                City = "City",
                CompletedDonationsAllTimeNo = 2,
                CompletedDonationsPastWeekNo = 1,
                CompletedDonationsAllTimePrice = 10,
                PendingDonationsAllTimeNo = 0,
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Producers.Add(producer);
            _context.Products.Add(product);
            _context.Applications.Add(application);
            _context.Applications.Add(application2);
            await _context.SaveChangesAsync();

            var userDTO = await _repository.FindAsync(id);
            var newDTO = userDTO as DetailedProducerDTO;

            Assert.Equal(expected.UserId, userDTO.UserId);
            Assert.Equal(expected.Email, userDTO.Email);
            Assert.Equal(expected.UserRole, userDTO.UserRole);
            Assert.Equal(expected.PairingLink, newDTO.PairingLink);
            Assert.Equal(expected.Street, newDTO.Street);
            Assert.Equal(expected.StreetNumber, newDTO.StreetNumber);
            Assert.Equal(expected.City, newDTO.City);
            Assert.Equal(expected.CompletedDonationsAllTimeNo, newDTO.CompletedDonationsAllTimeNo);
            Assert.Equal(expected.CompletedDonationsAllTimePrice, newDTO.CompletedDonationsAllTimePrice);
            Assert.Equal(expected.CompletedDonationsPastWeekNo, newDTO.CompletedDonationsPastWeekNo);
            Assert.Equal(expected.PendingDonationsAllTimeNo, newDTO.PendingDonationsAllTimeNo);
        }

        [Fact]
        public async Task FindAsync_given_non_existing_id_returns_Null()
        {
            var id = 1;

            var userDTO = await _repository.FindAsync(id);

            Assert.Null(userDTO);
        }

        [Fact]
        public async Task UpdateAsync_given_Receiver_User_returns_True()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Receiver
            };

            var receiver = new Receiver
            {
                UserId = id
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Receivers.Add(receiver);
            await _context.SaveChangesAsync();

            var dto = new ReceiverUpdateDTO
            {
                UserId = id,
                FirstName = "Test",
                SurName = "test",
                Email = "test@Test",
                Country = "CountryCode",
                Password = "12345678",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
            };

            var result = await _repository.UpdateAsync(dto);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_given_Producer_User_returns_True()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Producer
            };

            var producer = new Producer
            {
                UserId = id,
                PairingSecret = "secret",
                Street = "Test",
                StreetNumber = "Some number",
                City = "City"
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Producers.Add(producer);
            await _context.SaveChangesAsync();

            var dto = new UserUpdateDTO
            {
                UserId = id,
                FirstName = "Test",
                SurName = "test",
                Email = "test@Test",
                Country = "CountryCode",
                Password = "12345678",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
                Street = "Test",
                StreetNumber = "Some number",
                City = "City"
            };

            var result = await _repository.UpdateAsync(dto);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_given_User_no_role_returns_False()
        {
                var id = 1;

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    Password = "12345678",
                    FirstName = "test",
                    SurName = "test",
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
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

                _context.Users.Add(user);
                _context.UserRoles.Add(userEnumRole);
                _context.Receivers.Add(receiver);
                await _context.SaveChangesAsync();

                var dto = new ReceiverUpdateDTO
                {
                    UserId = id,
                    FirstName = "Test",
                    SurName = "test",
                    Email = "test@Test",
                    Country = "CountryCode",
                    Password = "12345678",
                    UserRole = "",
                };

                var result = await _repository.UpdateAsync(dto);

                Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_given_User_wrong_role_returns_False()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = "12345678",
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
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

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Receivers.Add(receiver);
            await _context.SaveChangesAsync();

            var dto = new ReceiverUpdateDTO
            {
                UserId = id,
                FirstName = "Test",
                SurName = "test",
                Email = "test@Test",
                Country = "CountryCode",
                Password = "12345678",
                UserRole = "Customer",
            };

            var result = await _repository.UpdateAsync(dto);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_given_DTO_updates_User_information()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "1234"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Receiver
            };

            var receiver = new Receiver
            {
                UserId = id
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Receivers.Add(receiver);
            await _context.SaveChangesAsync();

            var dto = new ReceiverUpdateDTO
            {
                UserId = id,
                FirstName = "Test test",
                SurName = "test Test",
                Email = user.Email,
                Country = "UK",
                Password = "1234",
                NewPassword = "123456789",
                Description = "Test Test",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
            };

            var update = await _repository.UpdateAsync(dto);

            var updatedUser = await _repository.FindAsync(id);

            var updatedPassword = (await _context.Users.FindAsync(dto.UserId)).Password;
            var passwordCheck = PasswordHasher.VerifyPassword(dto.Email, updatedPassword, dto.NewPassword);

            Assert.Equal(dto.FirstName, updatedUser.FirstName);
            Assert.Equal(dto.SurName, updatedUser.SurName);
            Assert.Equal(dto.Country, updatedUser.Country);
            Assert.Equal(dto.Description, updatedUser.Description);

            Assert.True(passwordCheck);
        }

        [Fact]
        public async Task UpdateAsync_given_NewPassword_under_8_Length_returns_False()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Receiver
            };

            var receiver = new Receiver
            {
                UserId = id
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Receivers.Add(receiver);
            await _context.SaveChangesAsync();

            var dto = new ReceiverUpdateDTO
            {
                UserId = id,
                FirstName = "Test test",
                SurName = "test Test",
                Email = user.Email,
                Country = "UK",
                Password = "12345678",
                NewPassword = "12345",
                Description = "Test Test",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
            };

            var update = await _repository.UpdateAsync(dto);

            Assert.False(update);
        }

        [Fact]
        public async Task UpdateAsync_given_Producer_change_wallet_updates_Wallet()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Producer
            };

            var Producer = new Producer
            {
                UserId = id,
                PairingSecret = "ABCD",
                Street = "Test",
                StreetNumber = "42",
                City = "City"
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Producers.Add(Producer);
            await _context.SaveChangesAsync();

            var dto = new UserUpdateDTO
            {
                UserId = id,
                FirstName = "test",
                SurName = "test",
                Email = "test@Test",
                Country = "CountryCode",
                Password = "12345678",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
                Wallet = "Test Wallet",
                Street = "Test",
                StreetNumber = "42",
                City = "City"
            };

            var boo = await _repository.UpdateAsync(dto);

            var updated = await _repository.FindAsync(id);
            var newDTO = updated as DetailedProducerDTO;
            Assert.True(boo);
            Assert.Equal(dto.Wallet, newDTO.Wallet);
        }

        [Fact]
        public async Task UpdateAsync_given_non_existing_id_returns_False()
        {
            var nonExistingUser = new UserUpdateDTO
            {
                UserId = 0,
                FirstName = "test",
                SurName = "tst",
                Email = "test@Test",
                Country = "CountryCode",
                Password = "1234",
            };

            var result = await _repository.UpdateAsync(nonExistingUser);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_given_invalid_dto_returns_False()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                FirstName = "Test",
                SurName = "Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Receiver
            };

            var receiver = new Receiver
            {
                UserId = id
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Receivers.Add(receiver);
            await _context.SaveChangesAsync();

            var dto = new ReceiverUpdateDTO
            {
                UserId = id,
                Email = "test@Test",
                Country = "CountryCode",
                Password = "12345678",
                UserRole = userEnumRole.UserRoleEnum.ToString(),
            };

            var result = await _repository.UpdateAsync(dto);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateDeviceAddressAsync_given_existing_secret_returns_true()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Producer
            };

            var Producer = new Producer
            {
                UserId = id,
                PairingSecret = "ABCD",
                Street = "Test",
                StreetNumber = "Some number",
                City = "City"
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Producers.Add(Producer);
            await _context.SaveChangesAsync();

            var dto = new UserPairingDTO
            {
                PairingSecret = "ABCD",
                DeviceAddress = "Test",
                WalletAddress = "EFGH",
            };

            var result = await _repository.UpdateDeviceAddressAsync(dto);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateDeviceAddressAsync_given_existing_secret_updates_producer()
        {
            var id = 1;

            var user = new User
            {
                Id = id,
                Email = "test@Test",
                Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                FirstName = "test",
                SurName = "test",
                Country = "CountryCode",
                Created = new DateTime(1, 1, 1, 1, 1, 1)
            };

            var userEnumRole = new UserRole
            {
                UserId = id,
                UserRoleEnum = UserRoleEnum.Producer
            };

            var Producer = new Producer
            {
                UserId = id,
                PairingSecret = "ABCD",
                Street = "Test",
                StreetNumber = "Some number",
                City = "City"
            };

            _context.Users.Add(user);
            _context.UserRoles.Add(userEnumRole);
            _context.Producers.Add(Producer);
            await _context.SaveChangesAsync();

            var dto = new UserPairingDTO
            {
                PairingSecret = "ABCD",
                DeviceAddress = "Test",
                WalletAddress = "EFGH",
            };

            await _repository.UpdateDeviceAddressAsync(dto);

            var p = await _context.Producers.FindAsync(Producer.UserId);

            Assert.Equal(dto.DeviceAddress, p.DeviceAddress);
            Assert.Equal(dto.WalletAddress, p.WalletAddress);
        }

        [Fact]
        public async Task UpdateDeviceAddressAsync_given_nonExisting_secret_returns_false()
        {
            var dto = new UserPairingDTO
            {
                PairingSecret = "ABCD",
                DeviceAddress = "Test"
            };

            var result = await _repository.UpdateDeviceAddressAsync(dto);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateImageAsync_given_folder_existing_id_and_image_updates_user_thumbnail()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var config = GetSecurityConfig();

                var folder = ImageFolderEnum.@static.ToString();
                var id = 1;
                var fileName = "file.png";
                var formFile = new Mock<IFormFile>();

                var imageWriter = new Mock<IImageWriter>();
                imageWriter.Setup(i => i.UploadImageAsync(folder, formFile.Object)).ReturnsAsync(fileName);

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var producer = new Producer
                {
                    UserId = id,
                    PairingSecret = "ABCD",
                    Street = "Test",
                    StreetNumber = "Some number",
                    City = "City"
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Producers.Add(producer);
                await context.SaveChangesAsync();

                var repository = new UserRepository(config, imageWriter.Object, context);

                var update = await repository.UpdateImageAsync(id, formFile.Object);

                var updatedUser = await context.Users.FindAsync(id);

                Assert.Equal(fileName, updatedUser.Thumbnail);
                Assert.Equal(fileName, update);
            }
        }

        [Fact]
        public async Task UpdateImageAsync_given_folder_existing_id_and_image_and_existing_image_Creates_new_image_and_Removes_old_thumbnail()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var config = GetSecurityConfig();

                var folder = ImageFolderEnum.@static.ToString();
                var id = 1;
                var oldFile = "oldFile.jpg";
                var fileName = "file.png";
                var formFile = new Mock<IFormFile>();

                var imageWriter = new Mock<IImageWriter>();
                imageWriter.Setup(i => i.UploadImageAsync(folder, formFile.Object)).ReturnsAsync(fileName);
                imageWriter.Setup(i => i.DeleteImage(folder, oldFile)).Returns(true);

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Thumbnail = oldFile,
                    Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var producer = new Producer
                {
                    UserId = id,
                    PairingSecret = "ABCD",
                    Street = "Test",
                    StreetNumber = "Some number",
                    City = "City"
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Producers.Add(producer);
                await context.SaveChangesAsync();

                var repository = new UserRepository(config, imageWriter.Object, context);

                var update = await repository.UpdateImageAsync(id, formFile.Object);

                imageWriter.Verify(i => i.UploadImageAsync(folder, formFile.Object));
                imageWriter.Verify(i => i.DeleteImage(folder, oldFile));
            }
        }

        [Fact]
        public async Task UpdateImageAsync_given_folder_existing_id_invalid_file_returns_Exception_with_error_message()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var config = GetSecurityConfig();

                var folder = ImageFolderEnum.@static.ToString();
                var id = 1;
                var oldFile = "oldFile.jpg";
                var error = "Invalid image file";
                var formFile = new Mock<IFormFile>();

                var imageWriter = new Mock<IImageWriter>();
                imageWriter.Setup(i => i.UploadImageAsync(folder, formFile.Object)).ThrowsAsync(new ArgumentException(error));

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Thumbnail = oldFile,
                    Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var producer = new Producer
                {
                    UserId = id,
                    PairingSecret = "ABCD",
                    Street = "Test",
                    StreetNumber = "Some number",
                    City = "City"
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Producers.Add(producer);
                await context.SaveChangesAsync();

                var repository = new UserRepository(config, imageWriter.Object, context);

                var ex = await Assert.ThrowsAsync<Exception>(() => repository.UpdateImageAsync(id, formFile.Object));

                Assert.Equal(error, ex.Message);
            }
        }

        [Fact]
        public async Task UpdateImageAsync_given_non_existing_id_returns_null()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var formFile = new Mock<IFormFile>();
                var config = GetSecurityConfig();
                var imageWriter = new Mock<IImageWriter>();
                var repository = new UserRepository(config, imageWriter.Object, context);

                var update = await repository.UpdateImageAsync(42, formFile.Object);

                Assert.Null(update);
            }
        }

        [Fact]
        public async Task GetCountProducersAsync_returns_number_of_producers()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;
                var otherId = 2;

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var producer = new Producer
                {
                    UserId = id,
                    PairingSecret = "ABCD",
                    Street = "Test",
                    StreetNumber = "Some number",
                    City = "City"
                };

                var user2 = new User
                {
                    Id = otherId,
                    Email = "other@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Password = PasswordHasher.HashPassword("other@Test", "abcdefgh"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole2 = new UserRole
                {
                    UserId = otherId,
                    UserRoleEnum = UserRoleEnum.Producer
                };

                var producer2 = new Producer
                {
                    UserId = otherId,
                    PairingSecret = "EFGH",
                    Street = "Test",
                    StreetNumber = "Some number",
                    City = "City"
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Producers.Add(producer);
                context.Users.Add(user2);
                context.UserRoles.Add(userEnumRole2);
                context.Producers.Add(producer2);
                await context.SaveChangesAsync();


                var formFile = new Mock<IFormFile>();
                var config = GetSecurityConfig();
                var imageWriter = new Mock<IImageWriter>();
                var repository = new UserRepository(config, imageWriter.Object, context);

                int count = await repository.GetCountProducersAsync();

                Assert.Equal(2, count);
            }
        }

        [Fact]
        public async Task GetCountReceiversAsync_returns_number_of_producers()
        {
            using (var connection = await CreateConnectionAsync())
            using (var context = await CreateContextAsync(connection))
            {
                var id = 1;
                var otherId = 2;

                var user = new User
                {
                    Id = id,
                    Email = "test@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Password = PasswordHasher.HashPassword("test@Test", "12345678"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole = new UserRole
                {
                    UserId = id,
                    UserRoleEnum = UserRoleEnum.Receiver
                };

                var receiver = new Receiver
                {
                    UserId = id,
                };

                var user2 = new User
                {
                    Id = otherId,
                    Email = "other@Test",
                    FirstName = "Test",
                    SurName = "Test",
                    Password = PasswordHasher.HashPassword("other@Test", "abcdefgh"),
                    Country = "CountryCode",
                    Created = new DateTime(1, 1, 1, 1, 1, 1)
                };

                var userEnumRole2 = new UserRole
                {
                    UserId = otherId,
                    UserRoleEnum = UserRoleEnum.Receiver
                };

                var receiver2 = new Receiver
                {
                    UserId = otherId,
                };

                context.Users.Add(user);
                context.UserRoles.Add(userEnumRole);
                context.Receivers.Add(receiver);
                context.Users.Add(user2);
                context.UserRoles.Add(userEnumRole2);
                context.Receivers.Add(receiver2);
                await context.SaveChangesAsync();


                var formFile = new Mock<IFormFile>();
                var config = GetSecurityConfig();
                var imageWriter = new Mock<IImageWriter>();
                var repository = new UserRepository(config, imageWriter.Object, context);

                int count = await repository.GetCountReceiversAsync();

                Assert.Equal(2, count);
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

        private IOptions<SecurityConfig> GetSecurityConfig()
        {
            SecurityConfig config = new SecurityConfig
            {
                Secret = "0d797046248eeb96eb32a0e5fdc674f5ad862cad",
            };
            return Options.Create(config as SecurityConfig);
        }

        private string ConstructPairingLink(string pairingSecret)
        {
            return "byteball:AymLnfCdnKSzNHwMFdGnTmGllPdv6Qxgz1fHfbkEcDKo@obyte.org/bb#" + pairingSecret;
        }
    }
}
