﻿using System;
using System.Threading.Tasks;
using PolloPollo.Entities;
using PolloPollo.Shared;
using PolloPollo.Shared.DTO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PolloPollo.Services.Utils;
using MimeKit;
using MailKit.Net.Smtp;

namespace PolloPollo.Services
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly PolloPolloContext _context;
        private readonly IEmailClient _emailClient;

        public ApplicationRepository(IEmailClient emailClient, PolloPolloContext context)
        {
            _emailClient = emailClient;
            _context = context;
        }

        /// <summary>
        /// Create application from ApplicationCreateDTO and return an ApplicationDTO
        /// </summary>
        public async Task<ApplicationDTO> CreateAsync(ApplicationCreateDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            var application = new Application
            {
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                Motivation = dto.Motivation,
                Created = DateTime.UtcNow,
                Status = ApplicationStatusEnum.Open
            };

            try
            {
                var createdApplication = _context.Applications.Add(application);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return null;
            }

            var receiver = (from u in _context.Users
                            where u.Id == application.UserId
                            select new
                            {
                                ReceiverName = u.FirstName + " " + u.SurName,
                                u.Country,
                                u.Thumbnail
                            }).FirstOrDefault();

            var product = (from p in _context.Products
                           where p.Id == application.ProductId
                           select new
                           {
                               ProductId = p.Id,
                               ProductTitle = p.Title,
                               ProductPrice = p.Price,
                               ProducerId = p.UserId
                           }).FirstOrDefault();

            var applicationDTO = new ApplicationDTO
            {
                ApplicationId = application.Id,
                ReceiverId = application.UserId,
                ReceiverName = receiver.ReceiverName,
                Country = receiver.Country,
                Thumbnail = receiver.Thumbnail,
                ProductId = product.ProductId,
                ProductTitle = product.ProductTitle,
                ProductPrice = product.ProductPrice,
                ProducerId = product.ProducerId,
                Motivation = application.Motivation,
                Status = application.Status,
                CreationDate = application.Created.ToString("yyyy-MM-dd HH:mm:ss"),
        };

            return applicationDTO;
        }

        /// <summary>
        /// Find an application by id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns name="ApplicationDTO"></returns>
        public async Task<ApplicationDTO> FindAsync(int applicationId)
        {
            var application = await (from a in _context.Applications
                                     where a.Id == applicationId
                                     select new ApplicationDTO
                                     {
                                         ApplicationId = a.Id,
                                         ReceiverId = a.UserId,
                                         ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                                         Country = a.User.Country,
                                         Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                                         ProductId = a.Product.Id,
                                         ProductTitle = a.Product.Title,
                                         ProductPrice = a.Product.Price,
                                         ProducerId = a.Product.UserId,
                                         Motivation = a.Motivation,
                                         Status = a.Status,
                                         DateOfDonation = a.DateOfDonation.ToString("yyyy-MM-dd"),
                                         CreationDate = a.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                                     }).SingleOrDefaultAsync();

            if (application == null)
            {
                return null;
            }

            return application;
        }

        /// <summary>
        /// Update state of application
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<(bool status, (bool emailSent, string emailError))> UpdateAsync(ApplicationUpdateDTO dto)
        {
            var application = await _context.Applications.
                FirstOrDefaultAsync(p => p.Id == dto.ApplicationId);

            (bool emailSent, string emailError) = (false, null);

            if (application == null)
            {
                return (false, (emailSent, emailError));
            }

            application.Status = dto.Status;
            application.LastModified = DateTime.UtcNow;

            if (dto.Status == ApplicationStatusEnum.Pending)
            {
                application.DateOfDonation = DateTime.UtcNow;

                // Send mail to receiver that product can be picked up
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == application.UserId);
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == application.ProductId);
                var producerId = product.UserId;
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == producerId);
                var producerAddress = producer.Zipcode != null
                                        ? producer.Street + " " + producer.StreetNumber + ", " + producer.Zipcode + " " + producer.City
                                        : producer.Street + " " + producer.StreetNumber + ", " + producer.City;
                (emailSent, emailError) = SendDonationEmail(receiver.Email, product.Title, producerAddress);

            }
            else if (dto.Status == ApplicationStatusEnum.Completed)
            {
                // Send thank you email to receiver
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == application.UserId);
                (emailSent, emailError) = SendThankYouEmail(receiver.Email);

                // Send confirmatoin mail to producer
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == application.ProductId);
                var producerId = product.UserId;
                var producerUser = await _context.Users.FirstOrDefaultAsync(p => p.Id == producerId);
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.ApplicationId == dto.ApplicationId);

                var (emailSent2, emailError2) = SendProducerConfirmation(producerUser.Email, receiver.FirstName, receiver.SurName, dto.ApplicationId.ToString(), product.Title, contract.Bytes, contract.Price, contract.SharedAddress);
            }
            else if (dto.Status == ApplicationStatusEnum.Open) {
                application.DateOfDonation = DateTime.MinValue;
            }
            else if (dto.Status == ApplicationStatusEnum.Open)
            {
                application.DonationDate = null;
            }

            await _context.SaveChangesAsync();

            return (true, (emailSent, emailError));
        }

        private (bool sent, string error) SendDonationEmail(string ReceiverEmail, string ProductName, string ProducerAddress)
        {
            string subject = "You received a donation on PolloPollo!";
            string body = $"Congratulations!\n\nA donation has just been made to fill your application for {ProductName}. You can now go and receive the product at the shop with address: {ProducerAddress}. You must confirm reception of the product when you get there.\n\nFollow these steps to confirm reception:\n-Log on to pollopollo.org\n-Click on your user and select \"profile\"\n-Change \"Open applications\" to \"Pending applications\"\n-Click on \"Confirm Receival\"\n\nAfter 10-15 minutes, the confirmation goes through and the shop will be notified of your confirmation.\n\nIf you have questions or experience problems, please join https://discord.pollopollo.org or write an email to pollopollo@pollopollo.org\n\nSincerely,\nThe PolloPollo Project";

            return _emailClient.SendEmail(ReceiverEmail, subject, body);
        }

        private (bool sent, string error) SendThankYouEmail(string ReceiverEmail)
        {
            string subject = "Thank you for using PolloPollo";
            string body = $"Thank you very much for using PolloPollo.\n\n" +
                    "If you have suggestions for improvements or feedback, please join our Discord server: https://discord.pollopollo.org and let us know.\n\n" +
                    "The PolloPollo project is created and maintained by volunteers. We rely solely on the help of volunteers to grow the platform.\n\n" +
                    "You can help us help more people by asking shops to join and add products that people in need can apply for." +
                    "\n\nWe hope you enjoyed using PolloPollo" +
                    "\n\nSincerely," +
                    "\nThe PolloPollo Project";

            return _emailClient.SendEmail(ReceiverEmail, subject, body);
        }

        private (bool sent, string error) SendProducerConfirmation(string ProducerEmail, string ReceiverFirstName, string ReceiverSurName, string ApplicationId, string ProductName, int? AmountBytes, int? AmountUSD, string SharedWallet)
        {
            string subject = $"{ReceiverFirstName} {ReceiverSurName} confirmed receipt of application #{ApplicationId}";
            string body = $"{ReceiverFirstName} {ReceiverSurName} has just confirmed receipt of the product {ProductName}.\n\n" +
                    $"The application ID is #{ApplicationId} and contains {AmountBytes} which is roughly {AmountUSD} at current rates.\n\n" +
                    $"To withdraw the money, open your Obyte Wallet and find the Smart Wallet address starting with {SharedWallet.Substring(0,4)}.\n\n" +
                    "Thank you for using PolloPollo and if you have suggestions for improvements, please join our Discord server: https://discord.pollopollo.org and let us know.\n\n" +
                    "The PolloPollo project is created and maintained by volunteers. We rely solely on the help of volunteers to grow the platform.\n\n" +
                    "You can help us help more people by adding more products or encouraging other shops to join and add their products that people in need can apply for." +
                    "\n\nWe hope you enjoyed using PolloPollo." +
                    "\n\nSincerely," +
                    "\nThe PolloPollo Project";

            return _emailClient.SendEmail(ProducerEmail, subject, body);
        }

            /// <summary>
            /// Retrieve all open applications
            /// </summary>
            /// <returns></returns>
            public IQueryable<ApplicationDTO> ReadOpen()
        {
            var entities = from a in _context.Applications
                           where a.Status == ApplicationStatusEnum.Open
                           orderby a.Created descending
                           select new ApplicationDTO
                           {
                               ApplicationId = a.Id,
                               ReceiverId = a.UserId,
                               ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                               Country = a.User.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                               ProductId = a.Product.Id,
                               ProductTitle = a.Product.Title,
                               ProductPrice = a.Product.Price,
                               ProducerId = a.Product.UserId,
                               Motivation = a.Motivation,
                               Status = a.Status,
                               CreationDate = a.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                           };

            return entities;
        }

        /// <summary>
        /// Retrieve all open applications matching some filter
        /// </summary>
        /// <returns></returns>
        public IQueryable<ApplicationDTO> ReadFiltered(string Country = "ALL", string City = "ALL")
        {
            var entities = from a in _context.Applications
                           where a.Status == ApplicationStatusEnum.Open
                           let product = _context.Products.Where(x => x.Id == a.ProductId).First()
                           where _context.Users.Where(u => u.Id == product.UserId).Select(u => u.Country).First().Equals(Country) || Country.Equals("ALL")
                           where _context.Producers.Where(u => u.UserId == product.UserId).Select(u => u.City).First().Equals(City) || City.Equals("ALL")
                           orderby a.Created descending
                           select new ApplicationDTO
                           {
                               ApplicationId = a.Id,
                               ReceiverId = a.UserId,
                               ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                               Country = a.User.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                               ProductId = a.Product.Id,
                               ProductTitle = a.Product.Title,
                               ProductPrice = a.Product.Price,
                               ProducerId = a.Product.UserId,
                               Motivation = a.Motivation,
                               Status = a.Status,
                               CreationDate = a.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                           };

            return entities;
        }

        /// <summary>
        /// Retrieve all completed applications
        /// </summary>
        /// <returns></returns>
        public IQueryable<ApplicationDTO> ReadCompleted()
        {
            var entities = from a in _context.Applications
                           where a.Status == ApplicationStatusEnum.Completed
                           orderby a.DateOfDonation descending
                           select new ApplicationDTO
                           {
                               ApplicationId = a.Id,
                               ReceiverId = a.UserId,
                               ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                               Country = a.User.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                               ProductId = a.Product.Id,
                               ProductTitle = a.Product.Title,
                               ProductPrice = a.Product.Price,
                               ProducerId = a.Product.UserId,
                               Motivation = a.Motivation,
                               Status = a.Status,
                               CreationDate = a.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                               DateOfDonation = a.DateOfDonation.ToString("yyyy-MM-dd"),
                           };

            return entities;
        }

        /// <summary>
        /// Retrieve all applications by specified receiver
        /// </summary>
        /// <param name="receiverId"></param>
        /// <returns></returns>
        public IQueryable<ApplicationDTO> Read(int receiverId)
        {
            var entities = from a in _context.Applications
                           where a.UserId == receiverId
                           orderby a.Created descending
                           select new ApplicationDTO
                           {
                               ApplicationId = a.Id,
                               ReceiverId = a.UserId,
                               ReceiverName = $"{a.User.FirstName} {a.User.SurName}",
                               Country = a.User.Country,
                               Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(a.User.Thumbnail),
                               ProductId = a.Product.Id,
                               ProductTitle = a.Product.Title,
                               ProductPrice = a.Product.Price,
                               ProducerId = a.Product.UserId,
                               Motivation = a.Motivation,
                               Status = a.Status,
                               DateOfDonation = a.DateOfDonation.ToString("yyyy-MM-dd"),
                               CreationDate = a.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                           };

            return entities;
        }

        public async Task<ContractInformationDTO> GetContractInformationAsync(int applicationId)
        {
            var application = await (from a in _context.Applications
                                     where a.Id == applicationId
                                     select new
                                     {
                                         a.Product.Price,
                                         a.Product.UserId
                                     }).FirstOrDefaultAsync();
            if (application == null)
            {
                return null;
            }

            var producer = await (from p in _context.Producers
                                  where p.UserId == application.UserId
                                  select new
                                  {
                                      p.DeviceAddress,
                                      p.WalletAddress
                                  }).FirstOrDefaultAsync();

            return new ContractInformationDTO
            {
                ProducerDevice = producer.DeviceAddress,
                ProducerWallet = producer.WalletAddress,
                Price = application.Price
            };
        }

        /// <summary>
        /// Delete an application by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns name="bool"></returns>
        public async Task<bool> DeleteAsync(int userId, int id)
        {
            var application = _context.Applications.Find(id);

            if (application == null)
            {
                return false;
            }

            if (userId != application.UserId)
            {
                return false;
            }

            if (application.Status != ApplicationStatusEnum.Open)
            {
                return false;
            }

            _context.Applications.Remove(application);

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Get list of countries in which open applications exist
        /// </summary>
        public IQueryable<string> GetCountries()
        {
            var countries = (from a in _context.Applications
                             where a.Status == ApplicationStatusEnum.Open
                             let product = _context.Products.Where(x => x.Id == a.ProductId).First()
                             select new
                             {
                                 (from u in _context.Users
                                  where u.Id == product.UserId
                                  select new
                                  { u.Country }).First().Country,
                             }).Select(c => c.Country).Distinct().OrderBy(x => x);

            return countries;
        }

        /// <summary>
        /// Get list of cities in a specified country in which open applications exist exist
        /// </summary>
        public IQueryable<string> GetCities(string country)
        {
            var cities = (from a in _context.Applications
                          where a.Status == ApplicationStatusEnum.Open
                          let product = _context.Products.Where(x => x.Id == a.ProductId).First()
                          where product.Country.Equals(country)
                          select new
                          {
                              (from u in _context.Producers
                               where u.UserId == product.UserId
                               select new
                               { u.City }).First().City,
                          }).Select(c => c.City).Distinct().OrderBy(x => x);

            return cities;
        }

    }
}