// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace JAvatarServer
{
    public class Options
    {
        public string PathBase { get; set; }
        public AuthorizationOptions OpenIdConnect { get; set; } = new AuthorizationOptions();
        public JAvatar.Options JAvatar { get; set; } = new JAvatar.Options();
        public HeaderOptions Header { get; set; } = new HeaderOptions();
    }

    public class AuthorizationOptions
    {
        public string Authority { get; set; }
        public string Audience { get; set; }
        public bool RequireHttpsMetadata { get; set; } = true;

    }

    public class HeaderOptions
    {
        public bool ShowHeaders { get; set; }
        public bool UseHsts { get; set; }
        public CorsPolicyOptions Cors { get; set; } = new CorsPolicyOptions();
        public SecurityHeaderOptions Security { get; set; } = new SecurityHeaderOptions();
        public ForwardHeaderOptions Forwarding { get; set; } = new ForwardHeaderOptions();
    }

    public class ForwardHeaderOptions
    {
        public int ForwardLimit { get; set; } = 1;
        public string KnownProxies { get; set; }
        public string KnownNetworks { get; set; }
        public string TargetHeaders { get; set; }
    }

    public class SecurityHeaderOptions
    {
        public string ContentSecurity { get; set; } = "default-src 'self' 'unsafe-inline'";
        public string XContentType { get; set; } = "nosniff";
        public string XFrame { get; set; } = "SAMEORIGIN";
    }

    public class CorsPolicyOptions
    {
        public string Name { get; set; } = "default";
        public string[] Origins { get; set; } = new string[]{};
        public string[] Methods { get; set; } = new string[]{};
        public string[] Headers { get; set; } = new string[]{};
        public bool AllowCredentials { get; set; } = true;

        public CorsPolicy Build()
        {
            CorsPolicyBuilder policy = new CorsPolicyBuilder();
            if (Origins.Length == 0) policy.AllowAnyOrigin(); else policy.WithOrigins(Origins);
            if (Methods.Length == 0) policy.AllowAnyMethod(); else policy.WithMethods(Origins);
            if (Headers.Length == 0) policy.AllowAnyHeader(); else policy.WithHeaders(Origins);
            if (AllowCredentials && Origins?.Length > 0) policy.AllowCredentials(); else policy.DisallowCredentials();
            return policy.Build();
        }
    }
}
