namespace Google_Search_App.Models

{
    public class SearchModel
	{
		public string SearchTerm { get; set; }

		public string ErrorMessage { get; set; }
		public List<SearchResults> SearchResults { get; set; }
	}

	public class SearchResults
	{
		public int Id { get; set; }
		public string Content { get; set; }

		public string Link { get; set; }

		public string Title { get; set; }

		public string Name { get; set; }
	}
}

