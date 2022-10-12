﻿using BLL.Autorization;
using BLL.Autorization.Abstract;
using BLL.Autorization.Concrete;
using BLL.Helpers.ConfigurationClasses;
using BLL.Services.Abstract;
using BLL.Services.Concrete;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BLL.Helpers.Dependencis

{
    public static class BLLDependensis
    {
        public static void SetBLLDependensis(this IServiceCollection services,
                                                  IConfiguration configuration)
        {
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            services.Configure<JWTOptions>(configuration.GetSection("JWTOptions"));
            JWTOptions jwtSettings = configuration.GetSection("JWTOptions").Get<JWTOptions>();
            services.AuthenticationJwtSettings(jwtSettings);

            services.AddScoped<IJWTTokenService, JWTTokenService>();
            services.AddScoped<IAccauntService, AccauntService>();
            services.AddScoped<IChatService, ChatService>();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.Configure<ImageConfiguration>(configuration.GetSection("ImagePaths"));
        }
    }
}
