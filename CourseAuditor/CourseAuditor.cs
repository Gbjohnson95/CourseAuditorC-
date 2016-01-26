using System;
using System.IO;
using System.Xml;

namespace CourseAuditor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			//CourseDocument test = new CourseDocument ();
			//test.loadDoc ("C:\\Users\\gbjohnson\\Desktop\\test.html", "Test Document", "16094", "123456");

			parseManifestAndRun ("C:\\Users\\gbjohnson\\Desktop\\CHILD");

			Console.ReadLine();
		}

		public static void parseManifestAndRun( String path ) {
			// Load Manifest file from specified path
			XmlDocument manifest = new XmlDocument();
			Console.WriteLine (path + "\\imsmanifest.xml");
			manifest.Load (path + "\\imsmanifest.xml");

			// Get Nodes
			XmlNodeList resourceElems = manifest.GetElementsByTagName("resource");
			XmlNodeList itemElems = manifest.GetElementsByTagName("item");

			// Declare some variables
			String title, ident, filepath;

			// Increment through the elements
			for (int i=0; i < resourceElems.Count; i++) {
				if ("content".Equals (resourceElems[i].Attributes ["d2l_2p0:material_type"].InnerText)) {
					filepath = resourceElems[i].Attributes ["href"].InnerText;
					for (int h = 0; h < itemElems.Count; h++) {
						if (itemElems [h].Attributes ["identifierref"].InnerText.Equals (resourceElems [i].Attributes ["identifier"].InnerText) && filepath.Contains(".html")) {
							title = itemElems [h].ChildNodes [0].InnerText;
							ident = itemElems [h].Attributes ["identifier"].InnerText;
							Console.WriteLine (title);
						}
					}
				}
			}
		}
	}
}
