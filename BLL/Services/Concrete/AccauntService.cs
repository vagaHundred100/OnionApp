﻿using AutoMapper;
using Azure.Storage.Blobs;
using BLL.Autorization.Abstract;
using BLL.Autorization.Concrete;
using BLL.DTO;
using DAL.Helpers;
using BLL.Helpers.ConfigurationClasses;
using BLL.Services.Abstract;
using DAL.Context;
using DAL.Domains;
using DAL.Repositories.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Concrete
{
    public class AccauntService : IAccauntService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IJWTTokenService _tokenService;
        private readonly JWTOptions _jwtSettings;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _enviranment;
        private readonly ImageConfiguration _imageConfig;
        public AccauntService(IJWTTokenService tokenService,
                              IOptionsSnapshot<JWTOptions> jwtSettings,
                              IMapper mapper,
                              UserManager<User> userRepository,
                              RoleManager<Role> roleManager,
                              IUserRoleRepository userRoleRepository,
                              IWebHostEnvironment enviranment,
                              IOptions<ImageConfiguration> imageOptions)
        {
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _mapper = mapper;
            _userManager = userRepository;
            _roleManager = roleManager;
            _userRoleRepository = userRoleRepository;
            _enviranment = enviranment;
            _imageConfig = imageOptions.Value;
        }

        public async Task<Responce> ActivateUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            user.IsEnabled = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.Forbidden;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }
            return new Responce();
        }

        public async Task<Responce> DeactivateUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            user.IsEnabled = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.Forbidden;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public async Task<Responce> ChangePasswordAsync(ChangePasswordDTO model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPass, model.NewPass);
            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.Forbidden;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public async Task<Responce> ResetPasswordAsync(RisetPasswordDTO model)
        {
            var user = await _userManager.FindByNameAsync(model.UserId);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPass);

            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.Forbidden;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public ResponceWithData<List<UserIndexDTO>> GetAllUsersAsync()
        {
            var response = new ResponceWithData<List<User>>();
            var users = _userManager.Users.Include(c => c.Image).ToList();

            var userIndexs = _mapper.Map<List<User>, List<UserIndexDTO>>(users);

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].Image != null)
                {
                    byte[] img = GetImage(users[i].Image);
                    if (img != null)
                    {
                        userIndexs[i].ProfileImage = img;
                    }

                }
            }
            return new ResponceWithData<List<UserIndexDTO>>() { Data = userIndexs };
        }

        public async Task<ResponceWithData<string>> LoginAsync(LoginDTO model)
        {
            var response = new ResponceWithData<string>();

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponseWithData<string>(new List<string>
                                                           {"Wrong username" }, code);
            }

            var isCorrect = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!isCorrect)
            {
                var code = (int)HttpStatusCode.Forbidden;
                return ResponseCreator.CreateUnsuccessifullResponseWithData<string>(new List<string> { "Wrong password" }, code);
            }

            var options = _mapper.Map<UserClaimsOptions>(user);
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateJwt(options, roles, _jwtSettings);
            response.Data = token;
            return response;
        }

        public async Task<Responce> RegisterAsync(RegisterDTO model)
        {
            var user = new User();

            user = _mapper.Map<RegisterDTO, User>(model, user);
            var image = CreateImage(model.ImageFile);

            if (image == null)
            {
                List<string> errorMessages = new List<string> { "Could not upload create image" };
                ResponseCreator.CreateUnsuccessifullResponse(errorMessages, (int)HttpStatusCode.BadRequest);
            }

            user.Image = image;
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                var code = (int)HttpStatusCode.Conflict;
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public async Task<Responce> AddUserToRoleAsync(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.BadRequest;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();


        }

        public async Task<Responce> RemoveUserFromRoleAsync(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.BadRequest;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public async Task<Responce> UpdateUserAsync(UpdateDTO model)
        {
            var users = _userManager.Users.Include(c => c.Image);
            var user = users.Where(c => c.UserName == model.UserName).SingleOrDefault();
            var updatedUser = _mapper.Map<UpdateDTO, User>(model, user);

            Image image;
            image = CreateImage(model.ImageFile);

            if (image == null)
            {
                List<string> errorMessages = new List<string> { "Could not upload image" };
                ResponseCreator.CreateUnsuccessifullResponse(errorMessages, (int)HttpStatusCode.BadRequest);
            }
            user.Image = image;
            user.SecurityStamp = Guid.NewGuid().ToString();
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.BadRequest;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();
        }

        public async Task<Responce> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                var code = (int)HttpStatusCode.NotFound;
                return ResponseCreator.CreateUnsuccessifullResponse(new List<string>
                                                   {"User with such name does not exist" }, code);
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var code = (int)HttpStatusCode.BadRequest;
                var errorMessages = result.Errors.Select(c => c.Description).ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessages, code);
            }

            return new Responce();

        }

       
        #region Image methods

        private string UploadImage(IFormFile file, string newFileName)
        {
            if (file.Length > 0)
            {
                string foldeerPath = Path.Combine(_enviranment.WebRootPath, _imageConfig.ImageFolder);
                if (!Directory.Exists(foldeerPath))
                {
                    Directory.CreateDirectory(foldeerPath);
                }

                string filePath = Path.Combine(foldeerPath, newFileName);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                    fileStream.Flush();
                }
                return filePath;
            }
            else
            {
                return null;
            }
        }

        private Image CreateImage(IFormFile file)
        {
            Image image;
            image = file == null ? CreateDefaultImage() : CreateNewImage(file);
            return image;

        }

        private Image CreateNewImage(IFormFile file)
        {
            string uniqueFileName = GenerateFileName(file.FileName);
            string newFilePath = UploadImage(file, uniqueFileName);
            return newFilePath == null ? null : new Image()
            {
                Name = uniqueFileName,
                FilePath = newFilePath
            };
        }

        private Image CreateDefaultImage()
        {
            string sourceFolderPath = Path.Combine(_enviranment.WebRootPath, _imageConfig.DefaultFolder);
            string sourceFilePath = Path.Combine(sourceFolderPath, _imageConfig.DefaultImage);
            string destFolderPath = Path.Combine(_enviranment.WebRootPath, _imageConfig.ImageFolder);
            string destFileName = GenerateFileName(sourceFilePath);
            string destFilePath = Path.Combine(destFolderPath, destFileName);
            System.IO.File.Copy(sourceFilePath, destFilePath);
            return new Image() { Name = destFileName, FilePath = destFilePath };
        }

        private string GenerateFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string uniqueName = fileName + Guid.NewGuid().ToString() + Path.GetExtension(filePath);
            return uniqueName;
        }

        private byte[] GetImage(Image img)
        {
            string path = img.FilePath;
            if (File.Exists(path))
            {
                byte[] val = File.ReadAllBytes(path);
                return val;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region SearchMethod
        public async Task<ResponceWithData<List<SearchResponseDTO>>> SearchAsync(SearchDTO model)
        {
            var quarebelUsers = _userManager.Users;
            var quarableRoles = _roleManager.Roles;
            var quarabeleUserRoles = _userRoleRepository.GetAll();
            var users =
                from user in _userManager.Users
                join ur in _userRoleRepository.GetAll()
                       on user.Id equals ur.UserId
                select new SearchResponseDTO
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = _roleManager.Roles
                            .Where(c => c.Id == ur.RoleId)
                            .Select(c => c.Name)
                            .ToList()
                };

            var startResult = users;

            var props = typeof(SearchDTO).GetProperties();
            foreach (var prop in props)
            {
                if (prop.Name != "Role")
                {
                    users = FilterWithWhere(users, prop, model);
                }
                else
                {
                    if (model.Role != null)
                    {
                        var results = users.Where(c => c.Roles.Any(r => r == model.Role));
                        users = results.Any() ? results : users;
                    }
                }
            }

            var endResultList = startResult == users ? new List<SearchResponseDTO>() : await users.ToListAsync();

            return new ResponceWithData<List<SearchResponseDTO>>() { Data = endResultList, Success = true };
        }

        private IQueryable<SearchResponseDTO> FilterWithWhere(IQueryable<SearchResponseDTO> collection,
                                                    PropertyInfo property, SearchDTO model)
        {
            var propertyName = property.Name;
            var modelPropVal = property.GetValue(model);

            if (modelPropVal == null) return collection;

            dynamic dynamicVal = CastValue(modelPropVal);
            if (dynamicVal == null) return collection;

            string condition = String.Format("{0} == \"{1}\"", propertyName, dynamicVal);
            var fillteredColl = collection.Where(condition);
            return fillteredColl.Any() ? fillteredColl : collection;
        }

        private dynamic CastValue(object value)
        {
            if (value is int) return value as int?;
            if (value is double) return value as double?;
            if (value is float) return value as float?;
            if (value is string) return value as string;
            return null;
        }

        #endregion
    }



    #region New Task From Samir
    //public async Task<ServiceResponce> ResetPassword(UserResetPasswordDTO userChangePasswordDTO)
    //{
    //    IdentityResult result = null;
    //    User user = _userManager.Users.SingleOrDefault(u => u.Id == userChangePasswordDTO.UserId);
    //    if (user == null)
    //    {
    //        //return new OperationResult(false, RequestResults.UserNotFound);
    //    }
    //    string passResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
    //    result = await _userManager.ResetPasswordAsync(user, passResetToken, userChangePasswordDTO.NewPassword);
    //    if (!result.Succeeded)
    //    {
    //        // return new OperationResult(false, RequestResults.NotSuccessful, result.Errors);
    //    }
    //    return new null;
    //}

    // metod olmali User profiler




    #endregion

}
