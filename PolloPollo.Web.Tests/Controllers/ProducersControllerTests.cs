﻿using MockQueryable.Moq;
using Moq;
using PolloPollo.Repository;
using PolloPollo.Shared;
using PolloPollo.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace PolloPollo.Web.Tests.Controllers
{
    public class ProducersControllerTests
    {
        [Fact]
        public async Task GetReturnsDTOs()
        {
            var dto = new ProducerDTO();
            var all = new[] { dto }.AsQueryable().BuildMock();
            var repository = new Mock<IProducerRepository>();
            repository.Setup(s => s.Read()).Returns(all.Object);

            var controller = new ProducersController(repository.Object);

            var result = await controller.Get();

            Assert.Equal(dto, result.Value.Single());
        }

        [Fact]
        public async Task GetGivenNonExistingIdReturnsNotFound()
        {
            var repository = new Mock<IProducerRepository>();

            var controller = new ProducersController(repository.Object);

            var get = await controller.Get(1);

            Assert.IsType<NotFoundResult>(get.Result);
        }


        [Fact]
        public async Task DeleteGivenExistingIdDeletesEntity()
        {
            var repository = new Mock<IProducerRepository>();

            var controller = new ProducersController(repository.Object);

            await controller.Delete(42);

            repository.Verify(s => s.DeleteAsync(42));
        }

        [Fact]
        public async Task DeleteReturnsNoContent()
        {
            var repository = new Mock<IProducerRepository>();
            
            repository.Setup(s => s.DeleteAsync(42)).ReturnsAsync(true);
            
            var controller = new ProducersController(repository.Object);

            var delete = await controller.Delete(42);

            Assert.IsType<NoContentResult>(delete);
        }

        [Fact]
        public async Task DeleteGivenNonExistingIdReturnsNotFound()
        {
            var repository = new Mock<IProducerRepository>();

            var controller = new ProducersController(repository.Object);

            var delete = await controller.Delete(42);

            Assert.IsType<NotFoundResult>(delete);
        }

    }    
}
