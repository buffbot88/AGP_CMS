# AGP_CMS Razor Pages - Pure .NET Architecture

This directory contains Razor Pages for the AGP_CMS system, replacing legacy PHP templates.

## Architecture

AGP_CMS now uses **pure .NET** architecture:
- **Razor Pages** (.cshtml/.cshtml.cs) for server-side rendered UI
- **Blazor Components** (.razor) for interactive client-side features
- **Kestrel** webserver serves all content dynamically
- **No PHP required** - all CMS features run as C# modules

## Directory Structure

```
Pages/CMS/
├── Forums/
│   ├── Index.cshtml         - Forum categories and forums list
│   └── Index.cshtml.cs      - Forum index page model
├── Blogs/
│   ├── Index.cshtml         - Blog posts list
│   └── Index.cshtml.cs      - Blog index page model
└── Profiles/
    ├── Index.cshtml         - User profile display
    └── Index.cshtml.cs      - Profile page model
```

## Blazor Components

```
Components/CMS/
├── ForumPost.razor          - Reusable forum post display component
└── BlogPostCard.razor       - Reusable blog post card component
```

## Usage

### Razor Pages

Razor Pages are accessed via URL routes:
- `/cms/forums` - Forum system
- `/cms/blogs` - Blog system
- `/cms/profiles/{username}` - User profiles

### Blazor Components

Blazor components can be embedded in Razor Pages or used standalone:

```razor
<ForumPost 
    Author="@Model.Post.Author"
    PostDate="@Model.Post.Date"
    Content="@Model.Post.Content"
    AllowEdit="@Model.CanEdit" />
```

## Integration with AGP_CMS Module

These Razor Pages integrate with the AGP_CMS module API:
- Forum data from `/api/forums`
- Blog data from `/api/blogs`
- Profile data from `/api/profiles`

The page models call AGP_CMS API endpoints to fetch data and render it using Razor syntax.

## Benefits Over PHP

1. **Type Safety** - Compile-time checking prevents many runtime errors
2. **Performance** - No external PHP-FPM process needed
3. **Maintainability** - C# ecosystem with excellent tooling
4. **Security** - Built-in XSS protection, CSRF tokens, etc.
5. **Modern Features** - Async/await, LINQ, dependency injection
6. **Single Runtime** - Everything runs in .NET, no language mixing

## Migration from PHP

If you have existing PHP templates, convert them to Razor Pages:

### PHP Template
```php
<?php foreach($posts as $post): ?>
    <h2><?= htmlspecialchars($post['title']) ?></h2>
    <p><?= htmlspecialchars($post['content']) ?></p>
<?php endforeach; ?>
```

### Razor Page
```razor
@foreach (var post in Model.Posts)
{
    <h2>@post.Title</h2>
    <p>@post.Content</p>
}
```

Razor automatically HTML-encodes output, preventing XSS vulnerabilities.

## Development

To add new CMS features:

1. Create a new Razor Page in appropriate directory
2. Create corresponding PageModel class
3. Integrate with AGP_CMS API
4. Add route in Program.cs if needed

## Testing

Razor Pages can be unit tested:
```csharp
var pageModel = new IndexModel();
pageModel.OnGet();
Assert.NotEmpty(pageModel.Categories);
```

## Documentation

- [Razor Pages Documentation](https://learn.microsoft.com/aspnet/core/razor-pages)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor)
- [AGP_CMS Module](../../AGP_CMS/README.md)

## Status

✅ **Production Ready** - Pure .NET architecture fully implemented
⚠️ **PHP Deprecated** - Legacy PHP support marked as obsolete
🚀 **Future** - Additional Blazor components for enhanced interactivity
