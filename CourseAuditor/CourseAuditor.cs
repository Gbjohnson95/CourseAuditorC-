using Ionic.Zip;
using System;
using System.IO;
using System.Xml;
/// <summary>
/// Made a lot of small changes. Only real changes so far are on UnzipFilesWithManifest
/// 
/// ChangeLog:
/// Removed the HtmlAgilityPack, System.Xml.XPath, and CsQuery usings. HtmlAgilityPack and CsQuery pack are used in CourseDocument and aren't needed in CourseAuditor.
/// Put a capital letter at the beginning of method/function names. That's the convention they make me follow, so I applied it here.
/// Replaced all instances of String with string. In Java the String is both the var and the object. C# has them seperated into string and String (think int vs. Integer)
/// Replaced the method/function comments with method/function summaries. This lets them show as docs (when you hover over the name). Not sure if Xamarin does that or not, similar to a java doc
/// Renamed StartReportFile to DeleteOldReportFile. Seemed to fit it's functionality better as it deleted a file instead of started, and PrintToFile creates if the report doesn't already exist.
/// Renamed stringBuild to reportString. Seemed like it described it's use better as well as changing the name away from stringBuilder (StringBuilder being a class)
/// 
/// Split UnzipAndRun into two functions - UnzipFilesWithManifest and RunFoldersWithManifest
/// Removed the manifest check from RunFoldersWithManifest. This is because the check is now being run before extraction happens. Anything extracted should have a manifest.
/// Removed the reportFile and interactions with it from UnzipFilesWithManifest and from RunFoldersWithManifest. It is now only referenced in ParseManifestAndRun. 
///     This should keep the code cleaner as well as avoid passing a non-config var through the entire code.
/// Added Ionic.Zip using. This is from the DotNetZip package. It doesn't seem to run into the hanging issue that was experienced using System.IO.Compression
/// </summary>
namespace CourseAuditor
{
    class MainClass
    {
        private static XmlDocument config;

        /// <summary>
        /// Takes multiple cmd arguments. Each argument is a path to a config file. This can be used to string runs together/use different settings for each run.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                ReadConfigAndRun(args[i]);
            }
            Console.Beep();
        }

        /// <summary>
        /// Reads in Config, and grabs the target folder and target report file
        /// </summary>
        public static void ReadConfigAndRun(string configPath)
        {
            config = new XmlDocument();
            config.Load(configPath);
            string topDirectory = GetTextNode(config.GetElementsByTagName("containerFolder")[0]);
            UnzipFilesWithManifest(topDirectory);
            RunFoldersWithManifest(topDirectory);
			
        }
        /// <summary>
        /// Given the target folder, it will unzip any zips, and then send folders with imsmanifest.xml into manifest parser.
        /// </summary>
        /// <param name="topDirectory"></param>
        /// <param name="reportFile"></param>
        public static void UnzipFilesWithManifest(string topDirectory)
        {
            string[] zipsList = Directory.GetFiles(topDirectory, "*.zip");
            string extractionDestination;

            // Extract zips --------------------------------------------------------------------------------
            if (zipsList.Length > 0)
            {
                Console.WriteLine("---------- EXTRACTING ZIPS           ----------\n");
                for (int i = 0; i < zipsList.Length; i++)
                {
                    ZipFile zipFolder = new ZipFile(zipsList[i]);
                    // Generate extact folder name
                    extractionDestination = zipFolder.Name.Replace(".zip", "");

                    // If that folder already exists, skip
                    if (!Directory.Exists(extractionDestination))
                    {
                        Console.WriteLine("About to extract  " + zipFolder.Name);
                        // If the zipFile doesn't have the manifest, don't extract. There won't be a manifest to parse
                        if (zipFolder.ContainsEntry("imsmanifest.xml"))
                        {
                            zipFolder.ExtractAll(extractionDestination);
                            Console.WriteLine("\tExtracted " + zipFolder.Name + "\n");
                        }
                        else
                        {
                            Console.WriteLine("\tManifest file was not found\n");
                        }
                    }
                }
                Console.WriteLine("---------- FINISHED EXTRACTING ZIPS  ----------\n\n");
            }
        }

        public static void RunFoldersWithManifest(string topDirectory)
        {
            // Run folders that contain a manifest  ---------------------------------------------------------
            Console.WriteLine("---------- AUDITING COURSES          ---------- ");
            string[] folders = Directory.GetDirectories(topDirectory);
            for (int h = 0; h < folders.Length; h++)
            {
                FileInfo extractedCourse = new FileInfo(folders[h]);
                try
                {
                    Console.WriteLine("Auditing: " + extractedCourse.FullName);
                    ParseManifestAndRun(folders[h]);
                }
                catch (XmlException e)
                {
                    Console.WriteLine("\tFailed to audit course " + extractedCourse.FullName);
                    Console.WriteLine("\tInvalid XML found. Moving On.");
                    Console.WriteLine("\tSource: " + e.Source);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to audit course " + extractedCourse.FullName);
                    Console.WriteLine("\tSource: " + e.TargetSite);
                }
            }
            Console.WriteLine("---------- FINISHED AUDITING COURSES ----------");
        }
		
        /// <summary>
        /// Given a course folder it reads in the manifest, and the config to generate a line.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="reportFile"></param>
        public static void ParseManifestAndRun(string path)
        {
            //Begins the report
            string reportFile = GetTextNode(config.GetElementsByTagName("outFile")[0]);
            DeleteOldReportFile(reportFile);
            PrintToFile(reportFile, GetHeader());

            // Load Manifest file from specified path
            XmlDocument manifest = new XmlDocument();
            manifest.Load(path + "\\imsmanifest.xml");

            // Declare some variables
            string reportString = "";
            string doctitle, ident, filepath, orgunitid, type;

            // Populate some variables
            XmlNodeList resourceElems = manifest.GetElementsByTagName("resource");
            XmlNodeList itemElems = manifest.GetElementsByTagName("item");
            orgunitid = manifest.GetElementsByTagName("manifest")[0].Attributes["identifier"].InnerText.Substring(4);

            // Declare a CourseDocument
            CourseDocument doc = new CourseDocument();

            XmlNodeList printItem = config.GetElementsByTagName("printItem");

            // Increment through the elements
            for (int i = 0; i < resourceElems.Count; i++)
            {
                if ("content".Equals(resourceElems[i].Attributes["d2l_2p0:material_type"].InnerText))
                { // If the item is a content page
                    filepath = resourceElems[i].Attributes["href"].InnerText; // Get the path
                    for (int h = 0; h < itemElems.Count; h++)
                    { // Increment through the items list
                        // If the item matches its resource item, and the document is infact an HTML pagem run!
                        if (itemElems[h].Attributes["identifierref"].InnerText.Equals(resourceElems[i].Attributes["identifier"].InnerText))
                        {
                            // Get the last set of data before running document
                            doctitle = itemElems[h].ChildNodes[0].InnerText;
                            ident = itemElems[h].Attributes["identifier"].InnerText;

                            // Get rid of commas
                            doctitle = doctitle.Replace(",", "");
                            doctitle = doctitle.Replace("\n", "");

                            // If said HTML file exists, run
                            if (File.Exists(Path.Combine(path, filepath)))
                            {
                                // Initialize the document
                                if (filepath.EndsWith("html"))
                                {
                                    doc.LoadDoc(Path.Combine(path, filepath), doctitle, orgunitid, ident);

                                    // Start pulling data from the document and putting it into a return string
                                    foreach (XmlNode node in printItem)
                                    {
                                        type = node.SelectSingleNode("query").Attributes["type"].InnerText;
                                        switch (type)
                                        {
                                            case "OUI":
                                                reportString += orgunitid + ",";// OrgUnitID of document
                                                break;
                                            case "DocTitle":
                                                reportString += doctitle + ",";// Title as the course displays it
                                                break;
                                            case "htmlTitle":
                                                reportString += doc.GetHtmlTitle() + ",";// HTML Title
                                                break;
                                            case "CSS":
                                                reportString += doc.CountQuery(GetTextNode(node.SelectSingleNode("query"))) + ","; // Gets a CSS Query
                                                break;
                                            case "RegEx":
                                                reportString += doc.CountRegEx(GetTextNode(node.SelectSingleNode("query"))) + ","; // Gets a RegEx
                                                break;
                                            case "Headers":
                                                reportString += doc.CheckHeaders() + ","; // Checks Header
                                                break;
                                            case "Link":
                                                reportString += doc.GenerateD2lLink() + ","; // Generates a D2L Link
                                                break;
                                        }
                                    }
                                    PrintToFile(reportFile, reportString + "\n");
                                    reportString = "";
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the text node from an element.
        /// </summary>
        public static string GetTextNode(XmlNode node)
        {
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

        /// <summary>
        /// Generates a header for the report from the config file.
        /// </summary>
        public static string GetHeader()
        {
            XmlNodeList printItems = config.GetElementsByTagName("printItem");
            string stringBuilder = "";
            foreach (XmlNode node in printItems)
            {
                stringBuilder += GetTextNode(node.SelectSingleNode("printTitle")) + ",";
            }
            return stringBuilder + "\n";
        }

        /// <summary>
        /// Appends text to a file
        /// </summary>
        public static void PrintToFile(string filePath, string content)
        {
            // I commented out the first part because it didn't seem necessary. The File.AppendText() will create the file if it doesn't exist anyway.

            //// Make the file if it doesn't exist
            //if (!File.Exists(file))
            //{
            //    using (StreamWriter sw = File.CreateText(file))
            //    {
            //    }
            //}
            // Write to file if it does

            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.Write(content);
            }
        }

        /// <summary>
        /// If a report of the same name is already there, delete it.
        /// </summary>
        public static void DeleteOldReportFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

        }
    }
}