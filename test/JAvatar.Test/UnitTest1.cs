// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace JAvatar.Test
{
    public class UnitTest1
    {

        [Theory]
        [InlineData("/javatar/profile/feedbcd-rest-of-guid")]
        [InlineData("/javatar/unit/3a58")]
        [InlineData("/javatar/company/ae01bcd-rest-of-guid.png")]
        public void TestPaths(string path)
        {
            // Generator.Initialize(new JAvatarOptions
            // {
            //     SpriteFile = Path.Combine("/Users", "jam","Documents","dev","JAvatar","src","JAvatar","JAvatars.png"),
            //     DefaultDimension = 80
            // });
            if (String.IsNullOrEmpty(Path.GetExtension(path)))
                path += ".png";

            Generator.WriteImage(
                path,
                new FileStream(
                    path.Split('/').Last(),
                    FileMode.Create
                )
            );

            Assert.True(true);
        }

        [Fact]
        public void TestPathStringCompare()
        {
            var p1 = new PathString("/javatar/jam");
            var p2 = new PathString("/javatar");

            Assert.True(p1.StartsWithSegments(p2));
        }

        [Theory]
        [InlineData("/javatar/test/test")]
        [InlineData("/javatar/test")]
        public void TestRegexPattern(string input)
        {
            var pathRegex = new Regex("/.*/.+");
            Assert.True(pathRegex.IsMatch(input));

        }

        [Theory]
        [InlineData("/javatar/")]
        [InlineData("/javatar")]
        public void BadTestRegexPattern(string input)
        {
            var pathRegex = new Regex("/.*/.+");
            Assert.False(pathRegex.IsMatch(input));

        }

        [Fact]
        public void NameNormalizes()
        {
            string name = "mil.army.arcyber.cpb.png";
            string norm = Path.GetFileNameWithoutExtension(name);
            Assert.True(norm == "mil.army.arcyber.cpb");
        }
    }

}
