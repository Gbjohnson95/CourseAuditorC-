using System;
using System.IO;
using System.Xml;

namespace CourseAuditor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			String header = "OrgUnitID,DocTitle,HTMLTitle,All Links,Spans,\n";
			PrintToFile ("C:\\Users\\gbjohnson\\Desktop\\CHILD\\NewReport.csv", header);
			parseManifestAndRun ("C:\\Users\\gbjohnson\\Desktop\\CHILD");
		}

		public static void parseManifestAndRun( String path ) {
			// Load Manifest file from specified path
			XmlDocument manifest = new XmlDocument();
			Console.WriteLine (path + "\\imsmanifest.xml");
			manifest.Load (path + "\\imsmanifest.xml");

			// Declare some variables
			String doctitle, ident, filepath, orgunitid;

			// Populate some variables
			XmlNodeList resourceElems = manifest.GetElementsByTagName("resource");
			XmlNodeList itemElems = manifest.GetElementsByTagName("item");
			orgunitid = manifest.GetElementsByTagName ("manifest") [0].Attributes ["identifier"].InnerText.Substring (4);

			// Declare a CourseDocument
			CourseDocument doc = new CourseDocument ();

			// Increment through the elements
			for (int i=0; i < resourceElems.Count; i++) {
				if ("content".Equals (resourceElems[i].Attributes ["d2l_2p0:material_type"].InnerText)) { // If the item is a content page
					filepath = resourceElems[i].Attributes ["href"].InnerText; // Get the path
					for (int h = 0; h < itemElems.Count; h++) { // Increment through the items list
						// If the item matches its resource item, and the document is infact an HTML pagem run!
						if (itemElems [h].Attributes ["identifierref"].InnerText.Equals (resourceElems [i].Attributes ["identifier"].InnerText) && filepath.Contains(".html")) {
							// Get the last set of data before running document
							doctitle = itemElems [h].ChildNodes [0].InnerText;
							ident = itemElems [h].Attributes ["identifier"].InnerText;

							if (File.Exists (path + "//" + filepath)) {
								// Initialize the document
								doc.loadDoc (path + "//" + filepath, doctitle, orgunitid, ident);

								// Start pulling data from the document and putting it into a return string
								PrintToFile ( path + "//NewReport.csv", 
									orgunitid + ","
									+ doctitle + ","
									+ doc.getHtmlTitle() + ","
									+ doc.CountQuery("//a") + ","
									+ doc.CountQuery ("//span") + ",\n"
								);
							}
						}
					}
				}
			}
		}


		public static void PrintToFile(String file, String content) {
			if (!File.Exists (file)) {
				using (StreamWriter sw = File.CreateText (file)) {
				}
			}
			using (StreamWriter sw = File.AppendText(file)) {
				sw.Write (content);
			}
		}
	}
}
