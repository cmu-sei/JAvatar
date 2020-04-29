// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using JAvatar;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationRouteBuilderExtensions
    {
        /// <summary>
        /// Default endpoint builder. Use if conventions apply to all methods
        /// </summary>
        public static IEndpointConventionBuilder MapJAvatar(
            this IEndpointRouteBuilder endpoints,
            string path = "/javatar"
        )
        {
            string route = $"{path}/{{**path}}";

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<JAvatarMiddleware>()
                .Build();

            return endpoints.Map(route, pipeline).WithDisplayName("JAvatar");
        }

        /// <summary>
        /// GET endpoint builder. Use if conventions apply just to GET
        /// </summary>
        public static IEndpointConventionBuilder MapJAvatarGet(
            this IEndpointRouteBuilder endpoints,
            string path = "/javatar"
        )
        {
            string route = $"{path}/{{**path}}";

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<JAvatarMiddleware>()
                .Build();

            return endpoints.MapGet(route, pipeline).WithDisplayName("JAvatarGet");
        }

        /// <summary>
        /// POST endpoint builder. Use if conventions apply just to POST
        /// </summary>
        public static IEndpointConventionBuilder MapJAvatarPost(
            this IEndpointRouteBuilder endpoints,
            string path = "/javatar"
        )
        {
            string route = $"{path}/{{**path}}";

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<JAvatarMiddleware>()
                .Build();

            return endpoints.MapPost(route, pipeline).WithDisplayName("JAvatarPost");
        }
    }
}
