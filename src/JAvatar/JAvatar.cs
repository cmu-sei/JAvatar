// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace JAvatar
{
    public static class Generator
    {
        private static Image<Rgba32>[,] map;
        private static int
            width = 0,
            height = 0,
            default_width = 0;
        private static string rootPath = "";
        private static string default_image = "default.png";
        private static bool _persist_image;
        private static SixLabors.ImageSharp.Formats.Png.PngEncoder _pngEncoder;
        public static void Initialize(Options options)
        {
            Stream resource = null;
            default_width = options.DefaultDimension;
            default_image = options.DefaultImage ?? "default.png";
            _persist_image = options.PersistImages;
            rootPath = options.RootPath;
            _pngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();

            if (!String.IsNullOrEmpty(options.SpriteFile)
                && File.Exists(options.SpriteFile))
            {
                resource = new FileStream(options.SpriteFile, FileMode.Open, FileAccess.Read);
            }
            else
            {
                var assembly = typeof(JAvatar.Generator).Assembly;
                resource = assembly.GetManifestResourceStream("JAvatar.JAvatars.png");
            }

            try
            {
                string javatarPath = System.IO.Path.Combine(rootPath, options.RoutePrefix);
                if (!Directory.Exists(javatarPath)) Directory.CreateDirectory(javatarPath);

                foreach (var f in options.Folders)
                {
                    string destPath = System.IO.Path.Combine(javatarPath, f.Name);
                    if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                }
            } catch {
                throw new Exception("Invalid JAvatar folders");
            }

            Initialize(resource);
            resource.Dispose();
        }

        public static void Initialize(Stream spriteStream)
        {
            //load sprite map
            map = new Image<Rgba32>[4,8];
            using(Image<Rgba32> canvas = Image.Load<Rgba32>(spriteStream))
            {
                int w = canvas.Width / 8,
                    h = canvas.Height / 4;

                width = w;
                height = h;

                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 8; j++)
                        map[i,j] = canvas.Clone(x => x.Crop(new Rectangle(j*w, i*h, w, h)));
            }
        }

        private static int ParseId(string path)
        {
            int id = 0;
            string[] segments = path.Split('/');
            if (segments.Last().Length > 3)
                int.TryParse(
                    segments.Last().Substring(0, 4),
                    System.Globalization.NumberStyles.HexNumber,
                    null, out id);

            if (id == 0)
                id = new Random().Next();

            return id;
        }

        public static void WriteImage(string path, Stream stream)
        {
            WriteImage(path, 0, stream);
        }

        public static void WriteImage(string path, int outSize, Stream stream)
        {
            if (map == null)
                Initialize(new Options());

            int size = (outSize > 0)
                ? outSize
                : (default_width > 0)
                    ? default_width
                    : (width > 0)
                        ? width
                        : 120;

            string dest = NormalizePath(path);
            if (!TryStaticFile(dest, size, stream))
            {
                using(Image<Rgba32> canvas = new Image<Rgba32>(new Configuration(), width, width, Rgba32.White))
                {
                    GenerateImage(canvas, ParseId(path));
                    if (_persist_image)
                    {
                        if (!Directory.Exists(System.IO.Path.GetDirectoryName(dest)))
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));
                        }
                        using (var fs = new StreamWriter(dest))
                        {
                            canvas.SaveAsPng(fs.BaseStream, _pngEncoder);
                        }
                    }
                    canvas.Mutate(x => x.Resize(size, size));
                    canvas.SaveAsPng(stream, _pngEncoder);
                }
            }
        }

        public static void GenerateImage(Image<Rgba32> canvas, int id)
        {
            if (map == null)
                Initialize(new Options());

            int q = id & 0x7,
                r = id >> 4 & 0x7,
                s = id >> 8 & 0x7,
                t = id >> 12 & 0x7;

            canvas.Mutate(x => x.DrawImage(map[0,q], new Point(0, 20), 1));
            canvas.Mutate(x => x.DrawImage(map[1,r], new Point(0, 100), 1));
            canvas.Mutate(x => x.DrawImage(map[2,s], new Point(0, 100), 1));
            canvas.Mutate(x => x.DrawImage(map[3,t], new Point(0, 170), 1));
            ApplyRoundedCorners(canvas, 20);
        }

        public static void ApplyRoundedCorners(Image<Rgba32> img, float cornerRadius)
        {
            IPathCollection corners = BuildCorners(img.Width, img.Height, cornerRadius);

            var graphicOptions = new GraphicsOptions(true) {
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
            };
            img.Mutate(x => x.Fill(graphicOptions, Rgba32.LimeGreen, corners));
        }

        public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);
            IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));
            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);
            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }

        private static string NormalizePath(string path)
        {
            //flip any relative url paths to fs paths
            path = String.Join(
                System.IO.Path.DirectorySeparatorChar.ToString(),
                path.Split(new char[] { '/', '\\'})
            );

            //add root directory if not present
            if (!String.IsNullOrEmpty(rootPath) && !path.StartsWith(rootPath))
                path = rootPath + path;

            return path;
        }

        private static bool TryStaticFile(string path, int outSize, Stream stream)
        {
            bool exists = File.Exists(path);
            if (!exists)
            {
                // try with extension (backwards compatibility)
                exists = File.Exists(path + ".png");

                if (exists)
                {
                    path += ".png";
                }
                else
                {
                    //check for default image in folder
                    path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), default_image);
                    exists = File.Exists(path);
                }
            }

            if (exists)
            {
                using(Image<Rgba32> canvas = Image.Load(File.ReadAllBytes(path)))
                {
                    canvas.Mutate(x => x.Resize(outSize, outSize));
                    canvas.SaveAsPng(stream);
                }
            }

            return exists;
        }
    }
}
