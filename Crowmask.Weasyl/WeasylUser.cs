namespace Crowmask.Weasyl {
	public class WeasylUserBase {
		public string login { get; set; }
		public int userid { get; set; }
	}

    public class WeasylUserProfile
	{
		public string username { get; set; }
		public string full_name { get; set; }
		public string profile_text { get; set; }
		public WeasylUserMedia media { get; set; }
		public string login_name { get; set; }
		public WeasylStatistics statistics { get; set; }
		public WeasylUserInfo user_info { get; set; }
		public string link { get; set; }
    }

    public class WeasylUserMedia
    {
        public IEnumerable<WeasylMediaFile> avatar { get; set; }
    }

    public class WeasylStatistics
    {
		public int submissions { get; set; }
    }

    public class WeasylUserInfo
	{
		public int? age { get; set; }
		public string gender { get; set; }
		public string location { get; set; }
        public Dictionary<string, IEnumerable<string>> user_links { get; set; }
	}
}
