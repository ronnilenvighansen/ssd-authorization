using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssd_authorization_solution.DTOs;
using ssd_authorization_solution.Entities;

namespace MyApp.Namespace;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CommentController : ControllerBase
{
    private readonly AppDbContext db;

    public CommentController(AppDbContext ctx)
    {
        this.db = ctx;
    }

    [HttpGet]
    public IEnumerable<CommentDto> Get([FromQuery] int? articleId)
    {
        var query = db.Comments.Include(x => x.Author).AsQueryable();
        if (articleId.HasValue)
            query = query.Where(c => c.ArticleId == articleId);
        return query.Select(CommentDto.FromEntity);
    }

    [HttpGet(":id")]
    public CommentDto? GetById(int id)
    {
        return db
            .Comments.Include(x => x.Author)
            .Select(CommentDto.FromEntity)
            .SingleOrDefault(x => x.Id == id);
    }

    [Authorize(Roles = "Subscriber")]
    [HttpPost]
    public CommentDto Post([FromBody] CommentFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var author = db.Users.Single(x => x.UserName == userName);
        var article = db.Articles.Single(x => x.Id == dto.ArticleId);
        var entity = new Comment
        {
            Content = dto.Content,
            Article = article,
            Author = author,
        };
        var created = db.Comments.Add(entity).Entity;
        db.SaveChanges();
        return CommentDto.FromEntity(created);
    }

    [Authorize(Roles = "Editor")]
    [HttpPut(":id")]
    public CommentDto Put(int id, [FromBody] CommentFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var entity = db
            .Comments.Include(x => x.Author)
            //.Where(x => x.Author.UserName == userName)
            .Single(x => x.Id == id);
        entity.Content = dto.Content;
        var updated = db.Comments.Update(entity).Entity;
        db.SaveChanges();
        return CommentDto.FromEntity(updated);
    }

    [Authorize(Roles = "Editor")]
    [HttpDelete(":id")]
    public CommentDto? DeleteComment(int id)
    {
        var comment = db.Comments
        .Include(c => c.Author)
        .SingleOrDefault(x => x.Id == id);

        if (comment == null)
            return null; 

        db.Comments.Remove(comment);
        db.SaveChanges();

        return CommentDto.FromEntity(comment); 
    }
}
