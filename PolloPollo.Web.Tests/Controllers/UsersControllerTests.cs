﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PolloPollo.Entities;
using PolloPollo.Repository;
using PolloPollo.Repository.Utils;
using PolloPollo.Shared;
using PolloPollo.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PolloPollo.Web.Tests
{
    public class UsersControllerTests
    {
        private Mock<ClaimsPrincipal> MockClaimsSecurity(int id)
        {
            //Create ClaimIdentity
            var claims = new List<Claim>()
            {
               new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            };
            var identity = new ClaimsIdentity(claims);

            //Mock claim to make the HttpContext contain one.
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(m => m.HasClaim(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(true);
           
            claimsPrincipalMock.Setup(m => m.Claims).Returns(claims);

            return claimsPrincipalMock;
        }

        [Fact]
        public async Task Authenticate_returns_authenticated_tuple()
        {
            var token = "verysecrettoken";
            var id = 1;

            var dto = new AuthenticateDTO
            {
                Email = "test@Test",
                Password = "1234",
            };

            var userDTO = new DetailedUserDTO
            {
                UserId = id,
                Email = dto.Email,
                UserRole = UserRoleEnum.Receiver.ToString(),
                FirstName = "test",
                SurName = "test"
            }; 

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.Authenticate(dto.Email, dto.Password)).ReturnsAsync((userDTO, token));

            var controller = new UsersController(repository.Object);

            var result = await controller.Authenticate(dto);

            Assert.Equal("verysecrettoken", result.Value.Token);
            Assert.Equal(userDTO.UserId, result.Value.UserDTO.UserId);
            Assert.Equal(userDTO.Email, result.Value.UserDTO.Email);
            Assert.Equal(userDTO.UserRole, result.Value.UserDTO.UserRole);
            Assert.Equal(userDTO.FirstName, result.Value.UserDTO.FirstName);
            Assert.Equal(userDTO.SurName, result.Value.UserDTO.SurName);
        }

        [Fact]
        public async Task Authenticate_wrong_password_Returns_BadRequest()
        {
            var token = "verysecrettoken";
            var id = 1;

            var user = new User
            {
                Email = "test@Test",
                Password = "1234",
            };
            var dto = new AuthenticateDTO
            {
                Email = "wrong@Test",
                Password = "wrongpassword",
            };

            var userDTO = new DetailedUserDTO
            {
                UserId = id,
                Email = dto.Email,
                UserRole = UserRoleEnum.Receiver.ToString(),
                FirstName = "test",
                SurName = "test"
            };

            var responseText = "Username or password is incorrect";

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.Authenticate(user.Email, user.Password)).ReturnsAsync((userDTO,token));

            var controller = new UsersController(repository.Object);

            var authenticate = await controller.Authenticate(dto);

            var result = authenticate.Result as BadRequestObjectResult;

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(responseText, result.Value);
        }

        [Fact]
        public async Task Post_With_Role_Receiver_Creates_and_returns_Receiver()
        {
            var id = 1;
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "test@Test",
                Password = "1234",
                UserRole = UserRoleEnum.Receiver.ToString(),
            };

            var expected = new TokenDTO {
                UserDTO = new DetailedUserDTO
                {
                    UserId = id,
                    UserRole = dto.UserRole
                },
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.CreateAsync(It.IsAny<UserCreateDTO>())).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as CreatedAtActionResult;
            var resultValue = result.Value as TokenDTO;

            repository.Verify(s => s.CreateAsync(dto));

            Assert.Equal("Get", result.ActionName);
            Assert.Equal(id, result.RouteValues["id"]);
            Assert.Equal(dto.UserRole, resultValue.UserDTO.UserRole);
            Assert.Equal(id, resultValue.UserDTO.UserId);
        }

        [Fact]
        public async Task Post_with_Role_Producer_creates_and_returns_Producer()
        {
            var id = 1;
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "test@Test",
                Password = "1234",
                UserRole = UserRoleEnum.Producer.ToString(),

            };

            var expected = new TokenDTO
            {
                UserDTO = new DetailedUserDTO
                {
                    UserId = id,
                    UserRole = dto.UserRole
                }
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.CreateAsync(It.IsAny<UserCreateDTO>())).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as CreatedAtActionResult;
            var resultValue = result.Value as TokenDTO;

            repository.Verify(s => s.CreateAsync(dto));

            Assert.Equal("Get", result.ActionName);
            Assert.Equal(id, result.RouteValues["id"]);
            Assert.Equal(dto.UserRole, resultValue.UserDTO.UserRole);
            Assert.Equal(id, resultValue.UserDTO.UserId);
        }

        [Fact]
        public async Task Post_With_no_Role_returns_BadRequest_with_error_message()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "test@Test",
                Password = "1234",
            };

            var responseText = "Users must have a assigned a valid role";

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as BadRequestObjectResult;

            Assert.IsType<BadRequestObjectResult>(post.Result);
            Assert.Equal(responseText, result.Value);
        }

        [Fact]
        public async Task Post_With_invalid_Role_returns_BadRequest_with_error_message()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "test@Test",
                Password = "1234",
                UserRole = "test"
            };

            var responseText = "Users must have a assigned a valid role";

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as BadRequestObjectResult;

            Assert.IsType<BadRequestObjectResult>(post.Result);
            Assert.Equal(responseText, result.Value);
        }

        [Fact]
        public async Task Post_With_existing_user_returns_Conflict()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "test@Test",
                Password = "1234",
                UserRole = UserRoleEnum.Producer.ToString()
            };

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as ConflictObjectResult;

            Assert.IsType<ConflictObjectResult>(post.Result);
            Assert.Equal("This Email is already registered", result.Value);
        }

        [Fact]
        public async Task Post_no_email_returns_BadRequest()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "Test",
                SurName = "Test",
                Email = "",
                Password = "1234",
                UserRole = UserRoleEnum.Producer.ToString()
            };

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as BadRequestResult;

            Assert.IsType<BadRequestResult>(post.Result);
        }

        [Fact]
        public async Task Post_empty_dto_returns_BadRequest()
        {
            var dto = new UserCreateDTO
            {
                FirstName = "",
                SurName = "",
                Email = "",
                Password = "",
                UserRole = UserRoleEnum.Producer.ToString()
            };

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var post = await controller.Post(dto);
            var result = post.Result as BadRequestResult;

            Assert.IsType<BadRequestResult>(post.Result);
        }

        [Fact]
        public async Task Get_with_existing_id_returns_user()
        {
            var input = 1;

            var expected = new DetailedUserDTO
            {
                UserId = input,
                FirstName = "Test",
                SurName = "Test",
                Country = "Test",
                Thumbnail = "test.png",
                City = "Test",
                Description = "test",
                UserRole = UserRoleEnum.Producer.ToString()
            };
            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            var get = await controller.Get(input);

            Assert.Equal(expected.FirstName, get.Value.FirstName);
            Assert.Equal(expected.SurName, get.Value.SurName);
            Assert.Equal(expected.Country, get.Value.Country);
            Assert.Equal(expected.City, get.Value.City);
            Assert.Equal(expected.Description, get.Value.Description);
            Assert.Equal(expected.UserRole, get.Value.UserRole);
            Assert.Equal(expected.Thumbnail, get.Value.Thumbnail);
        }

        [Fact]
        public async Task Get_with_existing_id_and_role_receiver_returns_receiver()
        {
            var input = 1;

            var expected = new DetailedProducerDTO
            {
                FirstName = "test",
                UserRole = UserRoleEnum.Receiver.ToString()
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            var get = await controller.Get(input);

            Assert.Equal(expected.UserRole, get.Value.UserRole);
            Assert.Equal(expected.FirstName, get.Value.FirstName);
        }

        [Fact]
        public async Task Get_with_existing_id_and_role_receiver_returns_producer()
        {
            var input = 1;

            var expected = new DetailedProducerDTO
            {
                FirstName = "Test",
                UserRole = UserRoleEnum.Producer.ToString(),
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            var get = await controller.Get(input);

            Assert.Equal(expected.FirstName, get.Value.FirstName);
            Assert.Equal(expected.UserRole, get.Value.UserRole);
        }

        [Fact]
        public async Task Get_with_non_existing_id_returns_NotFound()
        {
            var input = 1;

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            var get = await controller.Get(input);

            Assert.IsType<NotFoundResult>(get.Result);
        }

        [Fact]
        public async Task Me_with_existing_id_returns_user()
        {
            var input = 1;

            var expected = new DetailedUserDTO
            {
                UserId = input,
                Email = "test@Test",
                FirstName = "Test",
                UserRole = UserRoleEnum.Producer.ToString()
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(input);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var get = await controller.Me();

            Assert.Equal(expected.UserId, get.Value.UserId);
            Assert.Equal(expected.Email, get.Value.Email);
            Assert.Equal(expected.FirstName, get.Value.FirstName);
            Assert.Equal(expected.UserRole, get.Value.UserRole);
        }

        [Fact]
        public async Task Me_with_existing_id_and_role_receiver_returns_receiver()
        {
            var input = 1;

            var expected = new DetailedReceiverDTO
            {
                UserId = input,
                Email = "Test@test",
                FirstName = "test",
                UserRole = UserRoleEnum.Receiver.ToString()
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(input);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var get = await controller.Me();

            Assert.Equal(expected.UserId, get.Value.UserId);
            Assert.Equal(expected.Email, get.Value.Email);
            Assert.Equal(expected.UserRole, get.Value.UserRole);
            Assert.Equal(expected.FirstName, get.Value.FirstName);
        }

        [Fact]
        public async Task Me_with_existing_id_and_role_receiver_returns_producer()
        {
            var input = 1;

            var expected = new DetailedProducerDTO
            {
                UserId = input,
                Email = "Test@test",
                FirstName = "Test",
                UserRole = UserRoleEnum.Producer.ToString(),
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.FindAsync(input)).ReturnsAsync(expected);

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(input);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var get = await controller.Me();

            Assert.Equal(expected.UserId, get.Value.UserId);
            Assert.Equal(expected.Email, get.Value.Email);
            Assert.Equal(expected.FirstName, get.Value.FirstName);
            Assert.Equal(expected.UserRole, get.Value.UserRole);
        }

        [Fact]
        public async Task Me_with_non_existing_id_returns_NotFound()
        {
            var input = 1;

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(input);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var get = await controller.Me();

            Assert.IsType<NotFoundResult>(get.Result);
        }

        [Fact]
        public async Task Me_with_wrong_id_format_existing_id_returns_BadRequest()
        {
            var input = "test";

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            //Create ClaimIdentity
            var claims = new List<Claim>()
            {
               new Claim(ClaimTypes.NameIdentifier, input),
            };
            var identity = new ClaimsIdentity(claims);

            //Mock claim to make the HttpContext contain one.
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(m => m.HasClaim(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(true);

            claimsPrincipalMock.Setup(m => m.Claims).Returns(claims);
            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = claimsPrincipalMock.Object;

            var get = await controller.Me();

            Assert.IsType<BadRequestResult>(get.Result);
        }

        [Fact]
        public async Task Put_with_User_id_same_as_claim_calls_update()
        {
            var dto = new UserUpdateDTO
            {
                UserId = 1,
                FirstName = "test",
            };

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(dto.UserId);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            await controller.Put(dto);

            repository.Verify(s => s.UpdateAsync(dto));
        }

        [Fact]
        public async Task Put_with_different_User_id_as_claim_returns_Forbidden()
        {
            var dto = new UserUpdateDTO
            {
                UserId = 1,
                FirstName = "test",
            };

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(42);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var put = await controller.Put(dto);

            Assert.IsType<ForbidResult>(put);
        }

        [Fact]
        public async Task Put_with_non_existing_id_returns_NotFound()
        {
            var dto = new UserUpdateDTO
            {
                UserId = 1,
                FirstName = "test",
                NewPassword = "1234"
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(m => m.UpdateAsync(dto)).ReturnsAsync(false);

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(dto.UserId);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var put = await controller.Put(dto);

            Assert.IsType<NotFoundResult>(put);
        }

        [Fact]
        public async Task Put_with_valid_dto_returns_NoContent()
        {
            var dto = new UserUpdateDTO
            {
                UserId = 1,
                FirstName = "test",
            };

            var repository = new Mock<IUserRepository>();
            repository.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(true);


            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(dto.UserId);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            await controller.Put(dto);

            repository.Verify(s => s.UpdateAsync(dto));
        }

        [Fact]
        public async Task PutImage_with_valid_id_and_image_returns_relative_path_to_file()
        {
            var folder = "static";
            var id = 1;
            var idString = "1";
            var formFile = new Mock<IFormFile>();
            var fileName = "file.png";
            var expectedOutput = "static/file.png";

            var repository = new Mock<IUserRepository>();
            repository.Setup(r => r.UpdateImageAsync(folder, id, It.IsAny<IFormFile>())).ReturnsAsync(fileName);
            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(id);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var putImage = await controller.PutImage(idString, formFile.Object);
            var image = putImage.Result as OkObjectResult;

            Assert.Equal(expectedOutput, image.Value);
        }

        [Fact]
        public async Task PutImage_with_valid_id_and_image_returns_OKObjectResult()
        {
            var folder = "static";
            var id = 1;
            var idString = "1";
            var formFile = new Mock<IFormFile>();
            var fileName = "file.png";

            var repository = new Mock<IUserRepository>();
            repository.Setup(r => r.UpdateImageAsync(folder, id, It.IsAny<IFormFile>())).ReturnsAsync(fileName);
            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(id);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var putImage = await controller.PutImage(idString, formFile.Object);

            Assert.IsType<OkObjectResult>(putImage.Result);
        }

        [Fact]
        public async Task PutImage_with_different_User_id_as_claim_returns_Forbidden()
        {
            var formFile = new Mock<IFormFile>();
            var idString = "1";

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(42);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var put = await controller.PutImage(idString, formFile.Object);

            Assert.IsType<ForbidResult>(put.Result);
        }

        [Fact]
        public async Task PutImage_with_non_existing_user_and_valid_claim_returns_NotFoundObjectResult_and_message()
        {
            var formFile = new Mock<IFormFile>();
            var idString = "1";
            var id = 1;
            var folder = "static";
            var error = "User not found";

            var repository = new Mock<IUserRepository>();
            repository.Setup(r => r.UpdateImageAsync(folder, id, It.IsAny<IFormFile>())).ReturnsAsync(default(string));

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(id);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var put = await controller.PutImage(idString, formFile.Object);
            var notFound = put.Result as NotFoundObjectResult;

            Assert.IsType<NotFoundObjectResult>(put.Result);
            Assert.Equal(error, notFound.Value);
        }

        [Fact]
        public async Task PutImage_with_wrong_id_format_returns_BadRequest()
        {
            var formFile = new Mock<IFormFile>();
            var idString = "test";

            var repository = new Mock<IUserRepository>();

            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            //Create ClaimIdentity
            var claims = new List<Claim>()
            {
               new Claim(ClaimTypes.NameIdentifier, idString),
            };
            var identity = new ClaimsIdentity(claims);

            //Mock claim to make the HttpContext contain one.
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(m => m.HasClaim(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(true);

            claimsPrincipalMock.Setup(m => m.Claims).Returns(claims);
            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = claimsPrincipalMock.Object;

            var putImage = await controller.PutImage(idString, formFile.Object);

            Assert.IsType<BadRequestResult>(putImage.Result);
        }

        [Fact]
        public async Task PutImage_with_invalid_image_returns_BadRequestObjectResult()
        {
            var folder = "static";
            var id = 1;
            var idString = "1";
            var formFile = new Mock<IFormFile>();

            var repository = new Mock<IUserRepository>();
            repository.Setup(r => r.UpdateImageAsync(folder, id, It.IsAny<IFormFile>())).ThrowsAsync(new ArgumentException("Invalid image file"));
            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(id);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var putImage = await controller.PutImage(idString, formFile.Object);

            Assert.IsType<BadRequestObjectResult>(putImage.Result);
        }

        [Fact]
        public async Task PutImage_with_invalid_image_returns_InternalServerError()
        {
            var folder = "static";
            var id = 1;
            var idString = "1";
            var formFile = new Mock<IFormFile>();

            var repository = new Mock<IUserRepository>();
            repository.Setup(r => r.UpdateImageAsync(folder, id, It.IsAny<IFormFile>())).ThrowsAsync(new ArgumentException());
            var controller = new UsersController(repository.Object);

            // Needs HttpContext to mock it.
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var cp = MockClaimsSecurity(id);

            //Update the HttpContext to use mocked claim
            controller.ControllerContext.HttpContext.User = cp.Object;

            var putImage = await controller.PutImage(idString, formFile.Object);
            var image = putImage.Result as StatusCodeResult;

            Assert.IsType<StatusCodeResult>(putImage.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, image.StatusCode);
        }
    }
}
