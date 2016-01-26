using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml.XPath;

namespace CourseAuditor
{
	public class CourseDocument
	{
		private HtmlAgilityPack.HtmlDocument htmlDoc;
		
		public CourseDocument (String filePath)
		{
			htmlDoc = new HtmlAgilityPack.HtmlDocument();
			htmlDoc.Load (filePath);

			//* Test cases
			Console.WriteLine ("Spans: " + CountQuery("//span"));
			Console.WriteLine ("Regex: " + CountRegEx ("<span>"));
			Console.WriteLine ("Title: " + getHtmlTitle());
			Console.ReadLine(); // Gheto wait function
			//*/
		}

		public int CountQuery (String query) {
			return htmlDoc.DocumentNode.SelectNodes(query).Count;
		}

		public int CountRegEx (String pattern) {
			return Regex.Matches(htmlDoc.DocumentNode.OuterHtml, pattern).Count;
		}

		public String getHtmlTitle () {
			String title, rawTitle = htmlDoc.DocumentNode.SelectSingleNode ("//title").InnerText;
			if (rawTitle != null && rawTitle != "") {
				title = htmlDoc.DocumentNode.SelectSingleNode ("//title").InnerText;
			} else {
				title = "ERROR: NO TITLE FOUND";
			}
			return title;
		}

	}


}

