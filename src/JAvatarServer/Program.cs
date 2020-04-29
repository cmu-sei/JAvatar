// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace JAvatarServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "JAvatar";
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
