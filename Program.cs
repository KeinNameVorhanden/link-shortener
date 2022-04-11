using LiteDB;
using HashidsNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("db0.db"));

var app = builder.Build();

Hashids _hashIds = new Hashids("hashid-salt", 11);

app.MapPost("/create", (string url, ILiteDatabase _context) =>
{
    var db = _context.GetCollection<shortUrl>(BsonAutoId.Int32);
    var entry = db.Find(p => p.longUrl == url).FirstOrDefault();

    var id = 0;
    if (entry is null)
    {
        entry = new shortUrl
        {
            longUrl = url
        };
        id = db.Insert(entry);
       
    }
    else
    {
        id = entry.Id;
    }

    return Results.Created("ShortURL: ", _hashIds.Encode(id));
});

app.MapGet("/{shortUrl}", (string shortUrl, ILiteDatabase _context) =>
{
    var id = _hashIds.Decode(shortUrl);
    if (id.Length == 0) return Results.NotFound();
    var tempId = id[0];
    var db = _context.GetCollection<shortUrl>();
    var entry = db.Query().Where(x => x.Id.Equals(tempId)).ToList().FirstOrDefault();
    if (entry != null)
    {
        if (entry.longUrl != null) return Results.Redirect(entry.longUrl);
    }
    return Results.NoContent();
});

app.MapPut("/update", (string shortUrl, string url, ILiteDatabase _context) =>
{
    var id = _hashIds.Decode(shortUrl);
    if (id is null) return Results.NotFound();
    var tempId = id[0];
    var db = _context.GetCollection<shortUrl>();
    var entry = db.Query().Where(x => x.Id.Equals(tempId)).ToList().FirstOrDefault();
    if (entry != null)
    {
        entry.longUrl = url;
        db.Update(entry);
        return Results.Ok();
    }
    return Results.NotFound();
});

app.MapDelete("/delete", (string shortUrl, ILiteDatabase _context) =>
{
    var id = _hashIds.Decode(shortUrl);
    if (id is null) return Results.NotFound();
    var tempId = id[0];
    var db = _context.GetCollection<shortUrl>();
    var entry = db.Query().Where(x => x.Id.Equals(tempId)).ToList().FirstOrDefault();
    if (entry != null) return Results.Ok(db.Delete(tempId));
    return Results.NotFound();
});

app.Run();

public class shortUrl
{
    private int _id;
    private string? _longUrl;
    
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    public string? longUrl
    {
        get => _longUrl;
        set => _longUrl = value;
    }
}
