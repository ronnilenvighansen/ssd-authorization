using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssd_authorization_solution.DTOs;
using ssd_authorization_solution.Entities;

namespace MyApp.Namespace;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : ControllerBase
{
    private readonly AppDbContext db;

    public ArticleController(AppDbContext ctx)
    {
        this.db = ctx;
    }

    [Authorize(Roles = "Guest")]
    [HttpGet]
    public IEnumerable<ArticleDto> Get()
    {
        return db.Articles.Include(x => x.Author).Select(ArticleDto.FromEntity);
    }

    [Authorize(Roles = "Guest")]
    [HttpGet(":id")]
    public ArticleDto? GetById(int id)
    {
        return db
            .Articles.Include(x => x.Author)
            .Where(x => x.Id == id)
            .Select(ArticleDto.FromEntity)
            .SingleOrDefault();
    }

    [Authorize(Roles = "Writer")]
    [HttpPost]
    public ArticleDto Post([FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var author = db.Users.Single(x => x.UserName == userName);
        var entity = new Article
        {
            Title = dto.Title,
            Content = dto.Content,
            Author = author,
            CreatedAt = DateTime.Now
        };
        var created = db.Articles.Add(entity).Entity;
        db.SaveChanges();
        return ArticleDto.FromEntity(created);
    }

    [Authorize(Roles = "Writer")]
    [HttpPut("writer:id")]
    public ArticleDto? WriterPut(int id, [FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var article = db
            .Articles
            .Include(x => x.Author)
            .SingleOrDefault(x => x.Id == id);

        if (article == null)
            return null;  
       
        if (article.Author.UserName != userName)
            return null;  

        article.Title = dto.Title;
        article.Content = dto.Content;

        var updatedArticle = db.Articles.Update(article).Entity;
        db.SaveChanges();

        return ArticleDto.FromEntity(updatedArticle);
    }

    [Authorize(Roles = "Editor")]
    [HttpPut("editor:id")]
    public ArticleDto EditorPut(int id, [FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var entity = db
            .Articles
            .Include(x => x.Author)
            .Single(x => x.Id == id);
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        var updated = db.Articles.Update(entity).Entity;
        db.SaveChanges();
        return ArticleDto.FromEntity(updated);
    }

    [Authorize(Roles = "Editor")]
    [HttpDelete(":id")]
    public ArticleDto? Delete(int id)
    {
        var article = db.Articles
        .Include(c => c.Author)
        .SingleOrDefault(x => x.Id == id);

        if (article == null)
            return null;  

        db.Articles.Remove(article);
        db.SaveChanges();

        return ArticleDto.FromEntity(article);
    }
}
