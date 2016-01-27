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
			String header = "OrgUnitID,DocTitle,HTMLTitle,IL2 Links,Box Links,Benjamin Links,Static IL3 Links,Bad Target Attr,IL3 Images,CSS Bolds,Spans,Depricated Tags,IL2 Variables,Mentions of Saturday,Headers,Link,\n";
			String folder = args[0];
			String report = Path.Combine (folder, "report.csv");
			try {
				startReport (report, header);
			} catch (Exception e) {
				Console.WriteLine ("Error encountered trying to make report file.");
				Console.WriteLine ("\tSource: " + e.TargetSite);
			} 
			unZipAndRun (folder, report);
			Console.ReadKey ();
		}

		public static void unZipAndRun ( String topDir, String reportFile ) {
			String[] zipsList = Directory.GetFiles (topDir, "*.zip");
			String extractFolder;

			// Extract zips
			Console.WriteLine ("---------- EXTRACTING ZIPS           ----------\n");
			for (int i = 0; i < zipsList.Length; i++) {
				// Generate extact folder name
				extractFolder = Path.Combine (topDir, zipsList [i].Replace (".zip", ""));

				// If that folder already exists, skip
				if (!Directory.Exists (Path.Combine(topDir,extractFolder))) {
					System.IO.Directory.CreateDirectory (extractFolder);
					Console.WriteLine ("About to extract  " + new DirectoryInfo(zipsList [i]).Name);
					ZipFile.ExtractToDirectory (Path.Combine (topDir, zipsList [i]), extractFolder);
					Console.WriteLine ("\tExtracted " + new DirectoryInfo(zipsList [i]).Name + "\n");
				}
			}
			Console.WriteLine ("---------- FINISHED EXTRACTING ZIPS  ----------\n\n");

			// Run folders that contain a manifest
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

		public static void parseManifestAndRun( String path, String reportfile) {
			// Load Manifest file from specified path
			XmlDocument manifest = new XmlDocument();
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
						if (itemElems [h].Attributes ["identifierref"].InnerText.Equals (resourceElems [i].Attributes ["identifier"].InnerText)) {
							// Get the last set of data before running document
							doctitle = itemElems [h].ChildNodes [0].InnerText;
							ident = itemElems [h].Attributes ["identifier"].InnerText;

							// Get rid of commas
							doctitle = doctitle.Replace(",", "");
							doctitle = doctitle.Replace ("\n", "");

							// If said HTML file exists, run
							if (File.Exists (Path.Combine(path, filepath))) {
								// Initialize the document
								if (filepath.EndsWith ("html")) {
									doc.loadDoc (Path.Combine (path, filepath), doctitle, orgunitid, ident);

									// Start pulling data from the document and putting it into a return string
									PrintToFile (reportfile, 
										orgunitid + ","// OrgUnitID of document
										+ doctitle + ","// Title as the course displays it
										+ doc.getHtmlTitle () + ","// HTML Title
										+ doc.CountQuery ("a[href*='brainhoney']") + ","// IL2 Links
										+ doc.CountQuery ("a[href*='box.com']") + ","// Box Links
										+ doc.CountQuery ("a[href*='courses.byui.edu']") + ","// Benjamin Links
										+ doc.CountQuery ("a[href*=/home], a[href*=/viewContent], a[href*=/calendar]") + ","// Static IL3 Links
										+ doc.CountQuery ("a:not([target*='_blank'])") + ","// Links with bad targets
										+ doc.CountQuery ("img[src*='brainhoney']") + ","// IL2 Links
										+ doc.CountRegEx ("font-weight\\:bold") + ","// CSS Bolds
										+ doc.CountQuery ("span") + ","// Spans
										+ doc.CountQuery ("b, i, br") + ","// Depricated tags
										+ doc.CountRegEx ("\\$[A-Za-z]+\\S\\$") + ","// IL2 Varables
										+ doc.CountRegEx ("[sS]aturday") + ","// Mentions Saturday
										+ doc.checkHeaders () + ","// Verify headers comply with ADA
										+ doc.generateD2lLink () + ",\n" // Generate link to the document
									);
								}
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

		public static void startReport(String file, String header) {
			if (File.Exists (file)) {
				File.Delete (file);
				PrintToFile (file, header);
			} else {
				PrintToFile (file, header);
			}

		}
	}
}
