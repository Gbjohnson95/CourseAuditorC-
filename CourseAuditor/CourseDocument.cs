using System.Text.RegularExpressions;
using HtmlAgilityPack;
using CsQuery;

namespace CourseAuditor
{
    public class CourseDocument
    {
        private HtmlDocument htmlDoc;
        private string DocName, OrgUnitID, DocID;
        private CQ dom;

        public void LoadDoc(string filepath, string docname, string orgunitid, string docid)
        {
            // Load file into parser
            htmlDoc = new HtmlDocument();
            htmlDoc.Load(filepath);
            dom = CQ.Create(htmlDoc.DocumentNode.OuterHtml);

            // Set variables
            DocName = docname;
            OrgUnitID = orgunitid;
            DocID = docid;
        }

        // Counts a CSS query
        public int CountQuery(string query)
        {
            CQ result = dom.Select(query);
            return result.Length;
        }

        // Counts occureneces of a Regular Expression
        public int CountRegEx(string pattern)
        {
            return Regex.Matches(htmlDoc.DocumentNode.OuterHtml, pattern).Count;
        }

        // Gets the HTML title
        public string GetHtmlTitle()
        {
            string title, rawTitle = "";
            if (dom.Select("title") != null)
            {
                rawTitle = dom.Select("title").Html();
            }
            if (rawTitle != null && rawTitle != "")
            {
                title = rawTitle;
            }
            else
            {
                title = "ERROR: NO TITLE FOUND";
            }
            title = title.Replace(",", "");
            title = title.Replace("\n", "");
            return title;
        }

        // Checks the headers for ADA compliance
        public string CheckHeaders()
        {
            string headers = "";
            if (dom.Select("h1").Length > 0)
            {
                headers += "1";
            }
            if (dom.Select("h2").Length > 0)
            {
                headers += "2";
            }
            if (dom.Select("h3").Length > 0)
            {
                headers += "3";
            }
            if (dom.Select("h4").Length > 0)
            {
                headers += "4";
            }
            if (dom.Select("h5").Length > 0)
            {
                headers += "5";
            }
            if (dom.Select("h6").Length > 0)
            {
                headers += "6";
            }
            if (headers == "")
            {
                headers = "No Headers";
            }
            if ("123456".IndexOf(headers) == 0)
            {
                return "Good: " + headers;
            }
            else
            {
                return "Bad: " + headers;
            }
        }

        // Generates a working link to the document
        public string GenerateD2lLink()
        {
            return "https://byui.brightspace.com/d2l/le/content/" + OrgUnitID + "/viewContent/" + DocID + "/View";
        }


        public int ImageWidth()
        {
            //*
            int imgCounter = 0;
            string width;
            for (int i = 0; i < dom.Select("img").Length; i++)
            {
                if (dom.Select("img")[i].HasAttribute("width"))
                {
                    width = dom.Select("img")[i].GetAttribute("width");
                    if (width.Contains("%") && !dom.Select("img")[i].GetAttribute("src").Contains("banner"))
                    {
                        imgCounter++;
                    }
                    else
                    {
                        imgCounter++;
                    }
                }
            }
            return imgCounter;
            /*/
            return 5;
            //*/
        }
    }
}