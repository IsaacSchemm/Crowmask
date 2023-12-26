namespace Crowmask.Weasyl {
	public abstract class WeasylSubmissionBase {
		public string link { get; set; }
		public WeasylSubmissionMedia media { get; set; }
		public string owner { get; set; }
		public string owner_login { get; set; }
		public DateTime posted_at { get; set; }
		public string rating { get; set; }
		public string title { get; set; }
		public string type { get; set; }
	}

	public class WeasylGallerySubmission : WeasylSubmissionBase {
		public int submitid { get; set; }
		public string subtype { get; set; }
	}
	
	public abstract class WeasylSubmissionBaseDetail : WeasylSubmissionBase {
		public int comments { get; set; }
		public bool favorited { get; set; }
		public int favorites { get; set; }
		public bool friends_only { get; set; }
		public IEnumerable<string> tags { get; set; }
		public int views { get; set; }
	}

	public class WeasylSubmissionDetail : WeasylSubmissionBaseDetail {
		public int submitid { get; set; }
		public string subtype { get; set; }

		public string description { get; set; }
		public string embedlink { get; set; }
		public string folder_name { get; set; }
		public int? folderid { get; set; }
	}
}
