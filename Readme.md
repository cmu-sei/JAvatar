# JAvatar

AspNetCore middleware for serving generated and uploaded avatar images.

## Usage
Run the JAvatarServer project or add the middleware to another app.

Add reference to project:
```bash
dotnet add package JAvatar
```

Configure `Startup.cs`:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddJAvatar(() => new JAvatar.Options());
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEndpoints(opt => {
        opt.MapJAvatar(); // .RequireAuthorization();
    });
}
```

`Note:` If storing and serving user-uploaded avatar images as static files, consider whether to register JAvatar *before* or *after* StaticFiles.  If you want dynamically sized static images, register JAvatar first.  If you want to skip the dynamic resizing of static files, register StaticFiles first.  If you offload staticfiles to the web server (i.e. nginx tryfiles {}), you won't get dynamic sizing that JAvatar provides.  Exclude that folder from tryfiles.

## Settings
See `appsettings.json` for an example.

Authorization is defined when mapping the endpoints.  Map overloads exist if
you need to apply different conventions to different methods.

The user id claim type is configurable, but defaults to "sub".

You can add folders with a filename format and permission browse.
By default the root folder saves uploads as the user's id and disallows browsing. To override, specify the folder(s) and properties desired.  The root folder "name" is `/`.

## API
Request avatar images:
```
GET /javatar/<hex-string-subjectId-guid-or-hash>
```
`Note:` The route prefix is configurable, but the last segment should be a hex string (guid or hash).  If not, you'll get a random image with every request.

A request for an extensionless image will search for `<image>.png`

If the image doesn't exist, and a `default.png` exists in the same folder, the default will be returned.

Specify an image size with query param `x`.  Images are generated square, so only one pixel value is needed.
```
GET /javatar/123456789abcdef0123456789abc.png?x=80
```
`Note:` The default dimension can be configured.  Without configuration, it is the full size of the source sprite -- 250px.

## Sprites

The default sprite map is one I doodled very quickly.  To override it, set the SpriteFile option with the path to a sprite file.

The sprite file should be 4 rows and 8 columns and is composed as: top, middle, middle, bottom. So to generate an image, we take one column from each row and overlap the middle two rows.

The default sprite sheet is 2000x400, so each image in the map is 250x100.  Composing the 4 images targeted images results in an avatar sized 250x300 (because we overlap the middle rows) which we then trim to square at 250x250.  Then we resize to the desired dimensions.

Of course, quality goes down if you request dimensions too much greater than the original, since these aren't vector graphics at this point.

### Acknowlegements
This project relies on the great work of these projects:
* [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
* [ASP.NET Core](https://github.com/aspnet)
