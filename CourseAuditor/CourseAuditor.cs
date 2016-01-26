using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace CourseAuditor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// Write the header
			Sting header = "OrgUnitID,DocTitle,HTMLTitle,IL2 Links,Box Links,Benjamin Links,Spans,\n";
			PrintToFile ("C:\\Users\\gbjohnson\\Desktop\\CHILD\\NewReport.csv", header);

			// Run the course
			parseManifestAndRun ("C:\\Users\\gbjohnson\\Desktop\\CHILD");
		}

		public static void unZipAndRun ( String topDir ) {
			String[] zipsList = Directory.GetFiles (topDir, "*.zip");
			for (int i = 0; i < zipsList.GetLength; i++) {

			}


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

							// If said HTML file exists, run
							if (File.Exists (path + "//" + filepath)) {
								// Initialize the document
								doc.loadDoc (path + "//" + filepath, doctitle, orgunitid, ident);

								// Start pulling data from the document and putting it into a return string
								PrintToFile ( path + "//NewReport.csv", 
									orgunitid + ","
									+ doctitle + ","
									+ doc.getHtmlTitle() + ","
									+ doc.CountQuery("//a[contains(@href, 'brainhoney')]") + "," // IL2 Links
									+ doc.CountQuery("//a[contains(@href, 'box.com')]") + "," // Box Links
									+ doc.CountQuery("//a[contains(@href, 'courses.byui.edu')]") + "," // Benjamin Links
									+ doc.CountQuery("//a[contains(@href, '/home')] | //a[contains(@href, '/contentView')] | //a[contains(@href, '/calendar')]") + "," // Static Links
									+ doc.CountQuery("//a[not(@target, '_blank')]") + ","
									+ doc.CountQuery("//img[contains(@href, 'brainhoney')]") + ","
									+ doc.CountRegEx("font-weight\\:bold") + ","
									+ doc.CountQuery("//span") + ","
									+ doc.CountQuery("//b | //i | //br") + ","
									+ doc.CountRegEx("\\$[A-Za-z]+\\S\\$") + ","
									+ doc.CountRegEx("[sS]aturday") + ","
									+ doc.checkHeaders() + ","
									+ doc.generateD2lLink() + ","
									+ doc.CountQuery("//span") + ",\n"
								);
							}
						}
					}
				}
			}
		}


		public static void PrintToFile(String file, String content) {
			// Make the file if it doesn't exist
			if (!File.Exists (file)) {
				using (StreamWriter sw = File.CreateText (file)) {
				}
			}

			// Write to file if it does
			using (StreamWriter sw = File.AppendText(file)) {
				sw.Write (content);
			}
		}
	}
}
