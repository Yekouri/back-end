﻿using Microsoft.Extensions.Options;
using PolloPollo.Entities;
using PolloPollo.Shared;
using System;
using System.Linq;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PolloPollo.Services.Utils;
using PolloPollo.Shared.DTO;
using static PolloPollo.Shared.UserCreateStatus;

namespace PolloPollo.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly SecurityConfig _config;
        private readonly IPolloPolloContext _context;
        private readonly IImageWriter _imageWriter;
        private readonly string _deviceAddress;
        private readonly string _obyteHub;


        public UserRepository(IOptions<SecurityConfig> config, IImageWriter imageWriter, IPolloPolloContext context)
        {
            _config = config.Value;
            _imageWriter = imageWriter;
            _context = context;
            _deviceAddress = "AymLnfCdnKSzNHwMFdGnTmGllPdv6Qxgz1fHfbkEcDKo";
            _obyteHub = "obyte.org/bb";
        }

        /// <summary>
        /// Create a full user with a role and sub entity of the given role
        /// </summary>
        /// <param name="dto"></param>
        /// <returns name="TokenDTO"></returns>
        public async Task<(UserCreateStatus status, TokenDTO dto)> CreateAsync(UserCreateDTO dto)
        {
            if (dto is null) return (NULL_INPUT, null);
            if (String.IsNullOrEmpty(dto.FirstName) || String.IsNullOrEmpty(dto.SurName)) return (MISSING_NAME, null);
            if (String.IsNullOrEmpty(dto.Email)) return (MISSING_EMAIL, null);
            if (await (from u in _context.Users where u.Email == dto.Email select u).AnyAsync()) return (EMAIL_TAKEN, null);
            if (String.IsNullOrEmpty(dto.Password)) return (MISSING_PASSWORD, null);
            if (dto.Password.Length < 8) return (PASSWORD_TOO_SHORT, null);
            if (String.IsNullOrEmpty(dto.Country)) return (MISSING_COUNTRY, null);
            if (!Enum.IsDefined(typeof(UserRoleEnum), dto.UserRole)) return (INVALID_ROLE, null);

            // Creates initial DTO with the static
            // user information
            var userDTO = DTOBuilder.CreateDetailedUserDTO(dto);

            // Wrapped into a try catch as there are many DB restrictions
            // that need to be upheld to succeed with the transaction
            try
            {
                var user = DTOBuilder.CreateUser(dto, PasswordHasher.HashPassword(dto.Email, dto.Password));

                var createdUser = _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add the user to a role and add a foreign key for the ISA relationship
                // Used to extend the information on a user and give access restrictions
                if (dto.UserRole.Equals(nameof(UserRoleEnum.Producer)))
                {
                    var producerUserRole = DTOBuilder.CreateProducerUserRole(createdUser.Entity.Id);
                    _context.UserRoles.Add(producerUserRole);

                    var producer = DTOBuilder.CreateProducer(dto, createdUser.Entity.Id, GeneratePairingSecret());
                    _context.Producers.Add(producer);

                    await _context.SaveChangesAsync();

                    userDTO = DTOBuilder.CreateDetailedProducerDTO(dto, producer, _deviceAddress, _obyteHub);
                } 
                else if (dto.UserRole.Equals(nameof(UserRoleEnum.Receiver))) 
                {
                    userDTO.UserRole = UserRoleEnum.Receiver.ToString();

                    var receiverUserRole = DTOBuilder.CreateReceiverUserRole(createdUser.Entity.Id);
                    _context.UserRoles.Add(receiverUserRole);

                    var receiver = DTOBuilder.CreateReceiver(receiverUserRole.UserId);
                    _context.Receivers.Add(receiver);

                    await _context.SaveChangesAsync();
                } 
                userDTO.UserId = user.Id;
            }
            catch (Exception)
            {
                // Could also throw an exception for more information when failing the user creation
                return (UNKNOWN_FAILURE, null);
            }

            // Return the user information along with an authorized tokens
            // To login the user after creation
            return (SUCCESS, DTOBuilder.CreateTokenDTO(userDTO, (await Authenticate(dto.Email, dto.Password)).token));
        }

        /**
         * Based on a producer get the total number of donations (applications) their products have been a part of + the total price of these donations
         * Get for a specific application status
         * Get for either all time or past 'days' days
         */
        private async Task<(int, int)> GetDonationCountAndPriceOfStatusXForPastYDays(int userId, ApplicationStatusEnum status, bool allTime, int days=0) {
            TimeSpan pastDays = new TimeSpan(days, 0, 0, 0);
            DateTime sinceDate = DateTime.UtcNow - pastDays;

            var products = await (from p in _context.Products
                              where p.UserId == userId
                              select new
                              {
                                  p.Price,
                                  Applications =
                                    from a in p.Applications
                                    where a.Status == status && (a.LastModified >= sinceDate || allTime)
                                    select new {a.Id},
                              }).ToListAsync();

            if (products.Count == 0) {
                return (0, 0);
            }

            var donations = 0;
            var totalPrice = 0;

            foreach (var p in products)
            {
                var aCount = p.Applications.Count();
                donations += aCount;
                totalPrice += p.Price * aCount;
            }

            return (donations, totalPrice);
        }

        /// <summary>
        /// Fetches a user with all the information for a user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns name="DetailedUserDTO"></returns>
        public async Task<DetailedUserDTO> FindAsync(int userId)
        {
            // Fetches all the information for a user
            // Creates a complete profile with every property
            var fullUser = await (from u in _context.Users
                                  where u.Id == userId
                                  where u.UserRole.UserId == userId
                                  let role = u.UserRole.UserRoleEnum
                                  select new
                                  {
                                      UserId = u.Id,
                                      UserRole = role,
                                      Wallet = role == UserRoleEnum.Producer ?
                                                u.Producer.WalletAddress
                                                : default(string),
                                      Device = role == UserRoleEnum.Producer ?
                                                u.Producer.DeviceAddress
                                                : default(string),
                                      PairingSecret = role == UserRoleEnum.Producer ?
                                                u.Producer.PairingSecret
                                                : default(string),
                                      Street = role == UserRoleEnum.Producer ?
                                                u.Producer.Street
                                                : default(string),
                                      StreetNumber = role == UserRoleEnum.Producer ?
                                                u.Producer.StreetNumber
                                                : default(string),
                                      City = role == UserRoleEnum.Producer ?
                                                u.Producer.City
                                                : default(string),
                                      Zipcode = role == UserRoleEnum.Producer ?
                                                u.Producer.Zipcode
                                                : default(string),
                                      u.FirstName,
                                      u.SurName,
                                      u.Email,
                                      u.Country,
                                      u.Description,
                                      u.Thumbnail,
                                  }).SingleOrDefaultAsync();

            if (fullUser == null)
            {
                return null;
            }

            // Filter out the information based on the role
            // To only send back the profile information for the specific role
            switch (fullUser.UserRole)
            {
                case UserRoleEnum.Producer:
                    (int comPastWNo, int comPastWPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Completed, false, 7).Result;

                    (int comPastMNo, int comPastMPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Completed, false, 30).Result;

                    (int comAllNo, int comAllPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Completed, true).Result;

                    (int penPastWNo, int penPastWPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Pending, false, 7).Result;

                    (int penPastMNo, int penPastMPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Pending, false, 30).Result;

                    (int penAllNo, int penAllPrice) =
                        GetDonationCountAndPriceOfStatusXForPastYDays(fullUser.UserId, ApplicationStatusEnum.Pending, true).Result;

                    return new DetailedProducerDTO
                    {
                        UserId = fullUser.UserId,
                        Wallet = fullUser.Wallet,
                        Device = fullUser.Device,
                        PairingLink = !string.IsNullOrEmpty(fullUser.PairingSecret)
                            ? "byteball:" + _deviceAddress + "@" + _obyteHub + "#" + fullUser.PairingSecret
                            : default(string),
                        FirstName = fullUser.FirstName,
                        SurName = fullUser.SurName,
                        Email = fullUser.Email,
                        Street = fullUser.Street,
                        StreetNumber = fullUser.StreetNumber,
                        Zipcode = fullUser.Zipcode,
                        City = fullUser.City,
                        Country = fullUser.Country,
                        Description = fullUser.Description,
                        Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(fullUser.Thumbnail),
                        UserRole = fullUser.UserRole.ToString(),
                        CompletedDonationsPastWeekNo = comPastWNo,
                        CompletedDonationsPastWeekPrice = comPastWPrice,
                        CompletedDonationsPastMonthNo = comPastMNo,
                        CompletedDonationsPastMonthPrice = comPastMPrice,
                        CompletedDonationsAllTimeNo = comAllNo,
                        CompletedDonationsAllTimePrice = comAllPrice,
                        PendingDonationsPastWeekNo = penPastWNo,
                        PendingDonationsPastWeekPrice = penPastWPrice,
                        PendingDonationsPastMonthNo = penPastMNo,
                        PendingDonationsPastMonthPrice = penPastMPrice,
                        PendingDonationsAllTimeNo = penAllNo,
                        PendingDonationsAllTimePrice = penAllPrice
                    };
                case UserRoleEnum.Receiver:
                    return new DetailedReceiverDTO
                    {
                        UserId = fullUser.UserId,
                        FirstName = fullUser.FirstName,
                        SurName = fullUser.SurName,
                        Email = fullUser.Email,
                        Country = fullUser.Country,
                        Description = fullUser.Description,
                        Thumbnail = ImageHelper.GetRelativeStaticFolderImagePath(fullUser.Thumbnail),
                        UserRole = fullUser.UserRole.ToString()
                    };
                default:
                    // This should never happen, there cannot be an unknown role assigned.
                    return null;
            }
        }

        /// <summary>
        /// Updates information about a user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(UserUpdateDTO dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .Include(u => u.Producer)
                .Include(u => u.Receiver)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.Email == dto.Email);

            // Return null if user not found or password don't match
            if (user == null || !PasswordHasher.VerifyPassword(dto.Email, user.Password, dto.Password))
            {
                return false;
            }

            // Update user
            user.FirstName = dto.FirstName;
            user.SurName = dto.SurName;
            user.Country = dto.Country;
            user.Description = dto.Description;

            // If new password is set, hash the new password and update
            // the users password
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                if (dto.NewPassword.Length >= 8)
                {
                    // Important to hash the password
                    user.Password = PasswordHasher.HashPassword(dto.Email, dto.NewPassword);
                }
                else
                {
                    return false;
                }
            }

            // Role specific information updated here.
            if (dto.UserRole.Equals(nameof(UserRoleEnum.Producer)))
            {
                // Fields specified for producer is updated here
                if (user.Producer != null)
                {
                    if (!string.IsNullOrEmpty(dto.Wallet))
                    {
                        user.Producer.WalletAddress = dto.Wallet;
                    }
                    user.Producer.Street = dto.Street;
                    user.Producer.StreetNumber = dto.StreetNumber;
                    user.Producer.City = dto.City;
                    if (!string.IsNullOrEmpty(dto.Zipcode))
                    {
                        user.Producer.Zipcode = dto.Zipcode;
                    }
                }


            } else if (dto.UserRole.Equals(nameof(UserRoleEnum.Receiver)))
            {
                // Nothing to update since receivers has no extra fields
            } else { 
                return false;
            }

            try
            {
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }


        /// <summary>
        /// Updates information about a user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<bool> UpdateDeviceAddressAsync(UserPairingDTO dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .Include(u => u.Producer)
                .FirstOrDefaultAsync(u => u.Producer.PairingSecret == dto.PairingSecret);

            // Return null if user not found or password don't match
            if (user == null || user.UserRole.UserRoleEnum != UserRoleEnum.Producer)
            {
                return false;
            }

            // Update user
            user.Producer.DeviceAddress = dto.DeviceAddress;
            user.Producer.WalletAddress = dto.WalletAddress;

            try
            {
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Saves a new profile picture for a user on disk, and removes the old image from disk.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public async Task<string> UpdateImageAsync(int id, IFormFile image)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return null;
            }

            var folder = ImageFolderEnum.@static.ToString();

            var oldThumbnail = user.Thumbnail;

            try
            {
                var fileName = await _imageWriter.UploadImageAsync(folder, image);

                user.Thumbnail = fileName;

                await _context.SaveChangesAsync();

                // Remove old image
                if (oldThumbnail != null)
                {
                    _imageWriter.DeleteImage(folder, oldThumbnail);
                }

                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Creates an authentication token for a user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(UserAuthStatus status, DetailedUserDTO userDTO, string token)> Authenticate(string email, string password)
        {
            if (String.IsNullOrEmpty(email)) return (UserAuthStatus.MISSING_EMAIL, null, null);
            if (String.IsNullOrEmpty(password)) return (UserAuthStatus.MISSING_PASSWORD, null, null);

            var userEntity = await (from u in _context.Users
                                    where u.Email.Equals(email)
                                    select new 
                                    {u.Id, u.Password}).SingleOrDefaultAsync();
            

            if (userEntity == null) return (UserAuthStatus.NO_USER, null, null);

            var user = await FindAsync(userEntity.Id);

            var validPassword = PasswordHasher.VerifyPassword(email, userEntity.Password, password);
            if (!validPassword) return (UserAuthStatus.WRONG_PASSWORD, null, null);

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();

            // Import HmacSHa256 key to be used for creating a unique signing of the token
            // Defined in appsettings
            var key = Encoding.ASCII.GetBytes(_config.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    // Add information to Claim
                    new Claim(ClaimTypes.NameIdentifier, userEntity.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FirstName + " " + user.SurName),
                    new Claim(ClaimTypes.Role, user.UserRole)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                // Add unique signature signing to Token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var createdToken = tokenHandler.WriteToken(token);

            return (
                UserAuthStatus.SUCCESS,
                user,
                createdToken
                );
        }

        /// <summary>
        /// Retrieve count of producers
        /// </summary>
        public async Task<int> GetCountProducersAsync()
        {
            return await _context.Producers.CountAsync();
        }

        /// <summary>
        /// Retrieve count of receivers
        /// </summary>
        public async Task<int> GetCountReceiversAsync()
        {
            return await _context.Receivers.CountAsync();
        }

        private string GeneratePairingSecret()
        {
            return Guid.NewGuid().ToString() + "_" + DateTime.Now.Ticks;
        }
    }
}
