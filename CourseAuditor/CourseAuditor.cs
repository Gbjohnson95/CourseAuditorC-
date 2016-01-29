using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using HtmlAgilityPack;
using System.Xml.XPath;
using CsQuery;

namespace CourseAuditor
{
	class MainClass
	{
		private static XmlDocument config;

		// Feeds the config path into readConfigAndRun -------------------------------------------------------------------------------------------------
		public static void Main (string[] args) {
			for (int i = 0; i < args.Length; i++) {
				readConfigAndRun (args[i]);
			}
			Console.Beep();
		}

		// Reads in Config, and grabs the target folder and target report file -------------------------------------------------------------------------
		public static void readConfigAndRun(String configPath) {
				config = new XmlDocument();
				config.Load (configPath);
				unZipAndRun(getTextNode (config.GetElementsByTagName ("containerFolder")[0]), getTextNode(config.GetElementsByTagName ("outFile") [0]));
			
		}

		// Given the target folder, it will unzip any zips, and then send folders with imsmanifest.xml into manifest parser. ---------------------------
		public static void unZipAndRun ( String topDir, String reportFile ) {
			String[] zipsList = Directory.GetFiles (topDir, "*.zip");
			String extractFolder;
			startReportFile (reportFile);
			PrintToFile (reportFile, getHeader ());

			// Extract zips --------------------------------------------------------------------------------
			if (zipsList.Length > 0) {
				Console.WriteLine ("---------- EXTRACTING ZIPS           ----------\n");
				for (int i = 0; i < zipsList.Length; i++) {
					// Generate extact folder name
					extractFolder = Path.Combine (topDir, zipsList [i].Replace (".zip", ""));

					// If that folder already exists, skip
					if (!Directory.Exists (Path.Combine (topDir, extractFolder))) {
						System.IO.Directory.CreateDirectory (extractFolder);
						Console.WriteLine ("About to extract  " + new DirectoryInfo (zipsList [i]).Name);
						ZipFile.ExtractToDirectory (Path.Combine (topDir, zipsList [i]), extractFolder);
						Console.WriteLine ("\tExtracted " + new DirectoryInfo (zipsList [i]).Name + "\n");
					}
				}
				Console.WriteLine ("---------- FINISHED EXTRACTING ZIPS  ----------\n\n");
			}

			// Run folders that contain a manifest  ---------------------------------------------------------
			Console.WriteLine ("---------- AUDITING COURSES          ---------- ");
			String[] folders = Directory.GetDirectories(topDir);
			for (int h = 0; h < folders.Length; h++) {
				if (File.Exists (Path.Combine (folders [h], "imsmanifest.xml"))) {
					try {
						Console.WriteLine("Auditing: " + new DirectoryInfo(folders[h]).Name);
						parseManifestAndRun (folders[h], reportFile);
					} catch (System.Xml.XmlException e) {
						Console.WriteLine ("\tFailed to audit course " + new DirectoryInfo(folders [h]).Name);
						Console.WriteLine ("\tInvalid XML found. Moving On.");
						Console.WriteLine ("\tSource: " + e.Source);
					} catch (Exception e) {
						Console.WriteLine ("Failed to audit course " + new DirectoryInfo(folders [h]).Name);
						Console.WriteLine ("\tSource: " + e.TargetSite);
					}
				}
			}
			Console.WriteLine ("---------- FINISHED AUDITING COURSES ----------");
		}
		
		// Given a course folder it read in the manifest, and the config to generate a line ------------------------------------------------------------
		public static void parseManifestAndRun( String path, String reportfile) {
			// Load Manifest file from specified path
			XmlDocument manifest = new XmlDocument();
			manifest.Load (path + "\\imsmanifest.xml");

			// Declare some variables
			String doctitle, ident, filepath, orgunitid, stringBuilder = "", type;

			// Populate some variables
			XmlNodeList resourceElems = manifest.GetElementsByTagName("resource");
			XmlNodeList itemElems = manifest.GetElementsByTagName("item");
			orgunitid = manifest.GetElementsByTagName ("manifest") [0].Attributes ["identifier"].InnerText.Substring (4);

			// Declare a CourseDocument
			CourseDocument doc = new CourseDocument ();

			XmlNodeList printItem = config.GetElementsByTagName ("printItem");

			// Increment through the elements
			for (int i = 0; i < resourceElems.Count; i++) {
				if ("content".Equals (resourceElems [i].Attributes ["d2l_2p0:material_type"].InnerText)) { // If the item is a content page
					filepath = resourceElems [i].Attributes ["href"].InnerText; // Get the path
					for (int h = 0; h < itemElems.Count; h++) { // Increment through the items list
						// If the item matches its resource item, and the document is infact an HTML pagem run!
						if (itemElems [h].Attributes ["identifierref"].InnerText.Equals (resourceElems [i].Attributes ["identifier"].InnerText)) {
							// Get the last set of data before running document
							doctitle = itemElems [h].ChildNodes [0].InnerText;
							ident = itemElems [h].Attributes ["identifier"].InnerText;

							// Get rid of commas
							doctitle = doctitle.Replace (",", "");
							doctitle = doctitle.Replace ("\n", "");

							// If said HTML file exists, run
							if (File.Exists (Path.Combine (path, filepath))) {
								// Initialize the document
								if (filepath.EndsWith ("html")) {
									doc.loadDoc (Path.Combine (path, filepath), doctitle, orgunitid, ident);

									// Start pulling data from the document and putting it into a return string
									foreach (XmlNode node in printItem) {
										type = node.SelectSingleNode ("query").Attributes ["type"].InnerText;
										switch (type) {
										case "OUI":
											stringBuilder += orgunitid + ",";// OrgUnitID of document
											break;
										case "DocTitle":
											stringBuilder += doctitle + ",";// Title as the course displays it
											break;
										case "htmlTitle":
											stringBuilder += doc.getHtmlTitle () + ",";// HTML Title
											break;
										case "CSS":
											stringBuilder += doc.CountQuery (getTextNode (node.SelectSingleNode ("query"))) + ","; // Gets a CSS Query
											break;
										case "RegEx":
											stringBuilder += doc.CountRegEx (getTextNode (node.SelectSingleNode ("query"))) + ","; // Gets a RegEx
											break;
										case "Headers":
											stringBuilder += doc.checkHeaders () + ","; // Checks Header
											break;
										case "Link":
											stringBuilder += doc.generateD2lLink () + ","; // Generates a D2L Link
											break;
										}
									}
									PrintToFile (reportfile, stringBuilder + "\n");
									stringBuilder = "";
								}
							}
						}
					}
				}
			}
		}
		
		// Gets the text node from an element ----------------------------------------------------------------------------------------------------------
		public static String getTextNode (XmlNode node) {
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.NodeType == XmlNodeType.Text ||
					child.NodeType == XmlNodeType.CDATA)
				{
					return child.Value;
				}
			} 
			return "";
		}

		// Generates a header for the report from the config file --------------------------------------------------------------------------------------
		public static String getHeader () {
			XmlNodeList printItems = config.GetElementsByTagName ("printItem");
			String stringBuilder = "";
			foreach (XmlNode node in printItems) {
				stringBuilder += getTextNode (node.SelectSingleNode ("printTitle")) + ",";
			}
			return stringBuilder + "\n";
		}
		
		// Appends a file with text --------------------------------------------------------------------------------------------------------------------
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
		
		// If a report of the same name is already there, delete it ------------------------------------------------------------------------------------
		public static void startReportFile(String file) {
			if (File.Exists (file)) {
				File.Delete (file);
			}

		}
	}
}
