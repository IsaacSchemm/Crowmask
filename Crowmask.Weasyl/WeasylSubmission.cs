namespace CrosspostSharp3.Weasyl {
	public abstract class WeasylSubmissionBase {
		public string link;
		public WeasylSubmissionMedia media;
		public string owner;
		public string owner_login;
		public DateTime posted_at;
		public string rating;
		public string title;
		public string type;
	}

	public class WeasylGallerySubmission : WeasylSubmissionBase {
		public int submitid;
		public string subtype;
	}
	
	public abstract class WeasylSubmissionBaseDetail : WeasylSubmissionBase {
		public int comments;
		public bool favorited;
		public int favorites;
		public bool friends_only;
		public IEnumerable<string> tags;
		public int views;
	}

	public class WeasylSubmissionDetail : WeasylSubmissionBaseDetail {
		public int submitid;
		public string subtype;

		public string description;
		public string embedlink;
		public string folder_name;
		public int? folderid;
	}
}
