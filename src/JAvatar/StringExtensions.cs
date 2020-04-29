// Copyright 2020 Carnegie Mellon University. 
// Released under a MIT (SEI) license. See LICENSE.md in the project root. 

using System;
using Microsoft.AspNetCore.Http;

namespace JAvatar
{
    public static class StringExtensions
    {
        public static bool HasValue(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        public static string FullUrlPath(this HttpRequest r)
        {
            return r.Path.Value + r.QueryString.Value;
        }
    }
}
