// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

namespace JAvatar
{
    public class Options
    {
        public string RootPath { get; set; } = "./";
        public string RoutePrefix { get; set; } = "javatar";
        public string SpriteFile { get; set; }
        public string DefaultImage { get; set; } = "default.png";
        public int DefaultDimension { get; set; }
        public int CacheSeconds { get; set; } = 120;
        public int MaxFileBytes { get; set; } = 10485760;
        public bool PersistImages { get; set; } = false;
        public string IdClaim { get; set; } = "sub";
        public ImageFolder[] Folders { get; set; } = new ImageFolder[] {};
    }

    public class ImageFolder
    {
        public string Name { get; set; }
        public bool Browseable { get; set; }
        public FileNameMode NameMode { get; set; }
    }

    public enum FileNameMode
    {
        Normalized,
        Subject,
        SubjectAppend
    }
}
