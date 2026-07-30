using System.Data;
using System.Text.Json;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FashionHub.Web.Application.Chat;

public sealed class ChatConversationStore : IChatConversationStore
{
    private const string SessionKey = "FashionHub.Chat.Current";
    private const string SchemaCacheKey = "FashionHub.Chat.SchemaAvailable";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ChatConversationStore> logger;

    public ChatConversationStore(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache memoryCache,
        ILogger<ChatConversationStore> logger)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<ChatConversationDto> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        return await CanUseDatabaseHistoryAsync(cancellationToken)
            ? await GetDatabaseConversationAsync(cancellationToken)
            : GetSessionConversation();
    }

    public async Task AppendAsync(
        ChatMessageDto message,
        CancellationToken cancellationToken = default)
    {
        if (await CanUseDatabaseHistoryAsync(cancellationToken))
        {
            await AppendDatabaseMessageAsync(message, cancellationToken);
            return;
        }

        var conversation = GetSessionState();
        conversation.Messages.Add(message);
        TrimSessionMessages(conversation.Messages);
        SaveSessionState(conversation);
    }

    public async Task<ChatConversationDto> StartNewAsync(
        CancellationToken cancellationToken = default)
    {
        if (await CanUseDatabaseHistoryAsync(cancellationToken))
        {
            var userId = currentUser.UserId!.Value;
            var active = await dbContext.CuocTroChuyens
                .Where(item =>
                    item.IdnguoiDung == userId
                    && item.NgayKetThuc == null)
                .ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var item in active)
            {
                item.NgayKetThuc = now;
                item.NgayCapNhat = now;
            }

            var created = CreateDatabaseConversation(userId, now);
            dbContext.CuocTroChuyens.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ChatConversationDto(
                created.IdcuocTroChuyen.ToString("N"),
                IsPersistent: true,
                []);
        }

        var state = new SessionConversation(Guid.NewGuid().ToString("N"), []);
        SaveSessionState(state);
        return MapSession(state);
    }

    public async Task ClearCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (await CanUseDatabaseHistoryAsync(cancellationToken))
        {
            var userId = currentUser.UserId!.Value;
            var active = await dbContext.CuocTroChuyens
                .FirstOrDefaultAsync(
                    item =>
                        item.IdnguoiDung == userId
                        && item.NgayKetThuc == null,
                    cancellationToken);
            if (active != null)
            {
                dbContext.CuocTroChuyens.Remove(active);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        GetSession().Remove(SessionKey);
    }

    private async Task<ChatConversationDto> GetDatabaseConversationAsync(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var conversation = await dbContext.CuocTroChuyens
            .AsNoTracking()
            .Where(item =>
                item.IdnguoiDung == userId
                && item.NgayKetThuc == null)
            .OrderByDescending(item => item.NgayCapNhat)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            var created = CreateDatabaseConversation(userId, DateTime.UtcNow);
            dbContext.CuocTroChuyens.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ChatConversationDto(
                created.IdcuocTroChuyen.ToString("N"),
                IsPersistent: true,
                []);
        }

        var messages = await dbContext.TinNhanChats
            .AsNoTracking()
            .Where(message =>
                message.IdcuocTroChuyen == conversation.IdcuocTroChuyen)
            .OrderBy(message => message.NgayTao)
            .ThenBy(message => message.IdtinNhan)
            .Take(ChatLimits.MaxMessagesPerConversation)
            .ToListAsync(cancellationToken);

        return new ChatConversationDto(
            conversation.IdcuocTroChuyen.ToString("N"),
            IsPersistent: true,
            messages.Select(MapDatabaseMessage).ToList());
    }

    private async Task AppendDatabaseMessageAsync(
        ChatMessageDto message,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var conversation = await dbContext.CuocTroChuyens
            .FirstOrDefaultAsync(
                item =>
                    item.IdnguoiDung == userId
                    && item.NgayKetThuc == null,
                cancellationToken);
        if (conversation == null)
        {
            conversation = CreateDatabaseConversation(userId, DateTime.UtcNow);
            dbContext.CuocTroChuyens.Add(conversation);
        }

        var metadata = new ChatMessageMetadata(
            message.Products,
            message.Order,
            message.Actions);
        dbContext.TinNhanChats.Add(new TinNhanChat
        {
            IdcuocTroChuyen = conversation.IdcuocTroChuyen,
            VaiTro = message.Role,
            NoiDung = message.Content,
            DuLieuJson = message.Products.Count > 0
                || message.Order != null
                || message.Actions.Count > 0
                    ? JsonSerializer.Serialize(metadata, JsonOptions)
                    : null,
            NgayTao = message.SentAt
        });
        conversation.NgayCapNhat = message.SentAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        var excessMessages = await dbContext.TinNhanChats
            .Where(item =>
                item.IdcuocTroChuyen == conversation.IdcuocTroChuyen)
            .OrderByDescending(item => item.NgayTao)
            .ThenByDescending(item => item.IdtinNhan)
            .Skip(ChatLimits.MaxMessagesPerConversation)
            .ToListAsync(cancellationToken);
        if (excessMessages.Count > 0)
        {
            dbContext.TinNhanChats.RemoveRange(excessMessages);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> CanUseDatabaseHistoryAsync(
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return false;
        }

        if (!dbContext.Database.IsRelational())
        {
            return true;
        }

        if (memoryCache.TryGetValue(SchemaCacheKey, out bool available))
        {
            return available;
        }

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            var closeAfterProbe = connection.State != ConnectionState.Open;
            if (closeAfterProbe)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.CuocTroChuyen', N'U') IS NOT NULL "
                    + "AND OBJECT_ID(N'dbo.TinNhanChat', N'U') IS NOT NULL THEN 1 ELSE 0 END";
                var result = await command.ExecuteScalarAsync(cancellationToken);
                available = Convert.ToInt32(result) == 1;
            }
            finally
            {
                if (closeAfterProbe)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not verify chat history schema; using session history.");
            available = false;
        }

        memoryCache.Set(
            SchemaCacheKey,
            available,
            TimeSpan.FromMinutes(5));
        return available;
    }

    private ChatConversationDto GetSessionConversation()
    {
        return MapSession(GetSessionState());
    }

    private SessionConversation GetSessionState()
    {
        var json = GetSession().GetString(SessionKey);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<SessionConversation>(
                    json,
                    JsonOptions);
                if (existing != null)
                {
                    return existing;
                }
            }
            catch (JsonException)
            {
                logger.LogWarning("Invalid chat session data was discarded.");
            }
        }

        var created = new SessionConversation(Guid.NewGuid().ToString("N"), []);
        SaveSessionState(created);
        return created;
    }

    private void SaveSessionState(SessionConversation state)
    {
        GetSession().SetString(
            SessionKey,
            JsonSerializer.Serialize(state, JsonOptions));
    }

    private ISession GetSession()
    {
        return httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException(
                "Chat session is unavailable for the current request.");
    }

    private static ChatConversationDto MapSession(SessionConversation state)
    {
        return new ChatConversationDto(
            state.Id,
            IsPersistent: false,
            state.Messages);
    }

    private static ChatMessageDto MapDatabaseMessage(TinNhanChat message)
    {
        var metadata = DeserializeMetadata(message.DuLieuJson);
        return new ChatMessageDto(
            message.VaiTro,
            message.NoiDung,
            message.NgayTao,
            metadata.Products,
            metadata.Order,
            metadata.Actions);
    }

    private static ChatMessageMetadata DeserializeMetadata(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<ChatMessageMetadata>(
                    json,
                    JsonOptions);
                if (metadata != null)
                {
                    return metadata;
                }
            }
            catch (JsonException)
            {
                // A text-only history entry remains useful when old metadata is invalid.
            }
        }

        return new ChatMessageMetadata([], null, []);
    }

    private static CuocTroChuyen CreateDatabaseConversation(
        int userId,
        DateTime now)
    {
        return new CuocTroChuyen
        {
            IdcuocTroChuyen = Guid.NewGuid(),
            IdnguoiDung = userId,
            NgayTao = now,
            NgayCapNhat = now
        };
    }

    private static void TrimSessionMessages(List<ChatMessageDto> messages)
    {
        var excess = messages.Count - ChatLimits.MaxMessagesPerConversation;
        if (excess > 0)
        {
            messages.RemoveRange(0, excess);
        }
    }

    private sealed record SessionConversation(
        string Id,
        List<ChatMessageDto> Messages);

    private sealed record ChatMessageMetadata(
        IReadOnlyList<ChatProductDto> Products,
        ChatOrderDto? Order,
        IReadOnlyList<ChatActionDto> Actions);
}
