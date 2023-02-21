using Google_Search_App.Models;
using Google_Search_App.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;

namespace Google_Search_App.Controllers
{
    public class HomeController : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            //InsertToDb();
            SearchModel model = new SearchModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(SearchModel model)
        {
            try
            {
                model.ErrorMessage = string.Empty;
                List<SearchResults> dataModel = new List<SearchResults>();
                if (string.IsNullOrWhiteSpace(model.SearchTerm))
                {
                    model.ErrorMessage = "Search term is required.";
                    return View(model);
                }
                //UrlEncoder.encode
                var client = new Google.Apis.Customsearch.v1.CustomsearchService(new Google.Apis.Services.BaseClientService.Initializer
                { ApiKey = "AIzaSyBdyko-99iVks7XYUs7eV564pc9edVSUEg" });
                //var client = new Google.Apis.CustomSearchAPI.v1.CustomSearchAPIService(new Google.Apis.Services.BaseClientService.Initializer { ApiKey = "AIzaSyB9SIXnr2ylKDENV0l4GOUckAQ-jyfKFLU" });
                var listRequest = client.Cse.List();

                //listRequest.ExactTerms = model.SearchTerm;
                listRequest.Q = model.SearchTerm;
                listRequest.Cx = "f572fe735470b4b23";
                listRequest.Num = 10;
                int count = 1;
                while (dataModel != null)
                {
                    listRequest.Start = count;
                    listRequest.Execute().Items.ToList().ForEach(m => dataModel.Add(new SearchResults
                    {
                        Content = m.Snippet,
                        Link = m.Link,
                        Name = m.HtmlTitle,
                        Title = m.Title
                    }));

                    count = count + 10;

                    if (count >= 100)
                    {
                        break;
                    }
                }

                model.SearchResults = dataModel;
                //Insert search records in database.
                InsertToDb(dataModel);

                model = SearchFromDb(string.Empty);
                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Error Message: " + ex.Message;
            }

            return View(model);
        }

        public void InsertToDb(List<SearchResults> dataModel)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetValue<string>("ConnectionStrings:ConnStr")))
            {
                connection.Open();
                //First create table if already not exists.
                string query = "IF  NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'SearchResults' AND type = 'U') BEGIN CREATE TABLE SearchResults (Id INT IDENTITY(1,1), Url VARCHAR(MAX)) END";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.ExecuteNonQuery();//Execute query to create table.
                }

                //Delete all records from db table
                using (var delCommand = new SqlCommand("DELETE FROM SearchResults", connection))
                {
                    delCommand.ExecuteNonQuery();//Execute query to create table.
                }

                //Insert searched records in db table
                foreach (var item in dataModel)
                {
                    using (var command = new SqlCommand("INSERT INTO SearchResults (Url) VALUES (@Url)", connection))
                    {
                        command.Parameters.AddWithValue("@Url", item.Link);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public IActionResult Search()
        {
            SearchModel model = new SearchModel();

            return View(model);
        }

        [HttpPost]
        public IActionResult Search(SearchModel model)
        {
            model = SearchFromDb(model.SearchTerm);
            return View(model);
        }

        public SearchModel SearchFromDb(string searchTerm)
        {
            SearchModel model = new SearchModel();
            DataTable dtSearch = new DataTable();
            using (SqlConnection connection = new SqlConnection(_config.GetValue<string>("ConnectionStrings:ConnStr")))
            {
                connection.Open();
                string query = string.Empty;
                //Get records from database.
                if (!string.IsNullOrEmpty(searchTerm))
                    query = "SELECT * FROM SearchResults WHERE URL LIKE '%" + searchTerm.Trim() + "%'";
                else
                    query = "SELECT * FROM SearchResults";

                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    adapter.Fill(dtSearch);
                }
                connection.Close();
            }

            model.SearchResults = new List<SearchResults>();
            for (int count = 0; count < dtSearch.Rows.Count; count++)
            {
                model.SearchResults.Add(new SearchResults { Link = dtSearch.Rows[count]["Url"].ToString(), Id = Convert.ToInt32(dtSearch.Rows[count]["Id"]) });
            }

            return model;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}