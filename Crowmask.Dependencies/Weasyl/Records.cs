using Microsoft.FSharp.Collections;

namespace Crowmask.Dependencies.Weasyl
{
    public record WeasylWhoami(
        string login,
        int userid);

    public record WeasylMediaFile(
        int? mediaid,
        string url);

    public record WeasylUserMedia(
        FSharpList<WeasylMediaFile> avatar);

    public record WeasylStatistics(
        int submissions);

    public record WeasylUserInfo(
        int? age,
        string? gender,
        string? location,
        FSharpMap<string, FSharpList<string>> user_links);

    public record WeasylUserProfile(
        string username,
        string full_name,
        string profile_text,
        WeasylUserMedia media,
        string login_name,
        WeasylStatistics statistics,
        WeasylUserInfo user_info,
        string link);

    public record WeasylSubmissionMedia(
        FSharpList<WeasylMediaFile> submission,
        FSharpList<WeasylMediaFile> thumbnail);

    public record WeasylGallerySubmission(
        DateTime posted_at,
        int submitid);

    public record WeasylSubmissionDetail(
        string link,
        WeasylSubmissionMedia media,
        string owner,
        DateTime posted_at,
        string rating,
        string title,
        bool friends_only,
        FSharpSet<string> tags,
        int submitid,
        string description);

    public record WeasylJournalDetail(
        string link,
        string owner,
        DateTime posted_at,
        string rating,
        string title,
        bool friends_only,
        FSharpSet<string> tags,
        int journalid,
        string content);

    public record WeasylGallery(
        FSharpList<WeasylGallerySubmission> submissions,
        int? backid,
        int? nextid);
}
