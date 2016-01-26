using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Xml.XPath;

namespace CourseAuditor
{
	public class CourseDocument
	{
		private HtmlAgilityPack.HtmlDocument htmlDoc;
		private String DocName, OrgUnitID, DocID;

		public void loadDoc (String filepath, String docname, String orgunitid, String docid) {
			// Load file into parser
			htmlDoc = new HtmlAgilityPack.HtmlDocument();
			htmlDoc.Load (filepath);

			// Set variables
			DocName = docname;
			OrgUnitID = orgunitid;
			DocID = docid;
		}

		// Counts an XPath query
		public int CountQuery (String query) {
			if (htmlDoc.DocumentNode.SelectNodes (query) == null) {
				return 0;
			} else {
				return htmlDoc.DocumentNode.SelectNodes (query).Count;
			}
		}

		// Counts occureneces of a Regular Expression
		public int CountRegEx (String pattern) {
			return Regex.Matches(htmlDoc.DocumentNode.OuterHtml, pattern).Count;
		}

		// Gets the HTML title
		public String getHtmlTitle () {
			String title, rawTitle = htmlDoc.DocumentNode.SelectSingleNode ("//title").InnerText;
			if (rawTitle != null && rawTitle != "") {
				title = rawTitle;
			} else {
				title = "ERROR: NO TITLE FOUND";
			}
			return title;
		}

		// Checks the headers for ADA compliance
		public String checkHeaders () {
			String headers = "";
			if (htmlDoc.DocumentNode.SelectSingleNode("//h1") != null) {
				headers += "1";
			}
			if (htmlDoc.DocumentNode.SelectSingleNode("//h2") != null) {
				headers += "2";
			}
			if (htmlDoc.DocumentNode.SelectSingleNode("//h3") != null) {
				headers += "3";
			}
			if (htmlDoc.DocumentNode.SelectSingleNode("//h4") != null) {
				headers += "4";
			}
			if (htmlDoc.DocumentNode.SelectSingleNode("//h5") != null) {
				headers += "5";
			}
			if (htmlDoc.DocumentNode.SelectSingleNode("//h6") != null) {
				headers += "6";
			}
			if (headers == "") {
				headers = "No Headers";
			}
			if ("123456".IndexOf (headers) == 0) {
				return "Good: " + headers;
			} else {
				return "Bad: " + headers;
			}
		}

		// Generates a working link to the document
		public String generateD2lLink () {
			return "https://byui.brightspace.com/d2l/le/content/" + OrgUnitID + "/viewContent/" + DocID + "/View";
		}
	}


}

