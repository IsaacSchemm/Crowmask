namespace Crowmask.Weasyl {
	public class WeasylMediaFile {
		public int? mediaid { get; set; }
		public string url { get; set; }
	}

	public class WeasylSubmissionMedia {
		public IEnumerable<WeasylMediaFile> submission { get; set; }
	}
}
