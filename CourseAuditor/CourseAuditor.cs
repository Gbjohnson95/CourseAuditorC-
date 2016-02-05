using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
            CleanUp(ParseDirector(topDirectory));
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

        /// <summary>
        /// Directs the parsing of folders. Manages the exceptions and console output. Returns a list of succsfully parsed folders.
        /// </summary>
        /// <param name="topDirectory"></param>
        /// <returns></returns>
        public static List<FileInfo> ParseDirector(string topDirectory)
        {
            // Run folders that contain a manifest  ---------------------------------------------------------
            Console.WriteLine("---------- AUDITING COURSES          ---------- ");
            string[] folders = Directory.GetDirectories(topDirectory);
            List<FileInfo> parsedFiles = new List<FileInfo>();
            for (int h = 0; h < folders.Length; h++)
            {
                FileInfo unzippedCourse = new FileInfo(folders[h]);
                try
                {
                    Console.WriteLine("Auditing: " + unzippedCourse.FullName);
                    ParseManifestAndRun(folders[h]);
                    parsedFiles.Add(new FileInfo(unzippedCourse.FullName));
                }
                catch (XmlException e)
                {
                    Console.WriteLine("\tFailed to audit course " + unzippedCourse.FullName);
                    Console.WriteLine("\tInvalid XML found. Moving On.");
                    Console.WriteLine("\tSource: " + e.Source);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to audit course " + unzippedCourse.FullName);
                    Console.WriteLine("\tSource: " + e.TargetSite);
                }
            }
            Console.WriteLine("---------- FINISHED AUDITING COURSES ----------");
            // If a parse failes, let the user know how many failed.
            if (folders.Length > parsedFiles.Count)
            {
                Console.WriteLine(folders.Length - parsedFiles.Count + " File/s did not successfully parse.");
                Console.ReadKey();
            }
            // Return a list of all the successfully parsed courses
            return parsedFiles;
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
            string doctitle, ident, filepath, orgUnitId, type;

            // Populate some variables
            XmlNodeList resourceElements = manifest.GetElementsByTagName("resource");
            XmlNodeList itemElements = manifest.GetElementsByTagName("item");
            orgUnitId = manifest.GetElementsByTagName("manifest")[0].Attributes["identifier"].InnerText.Substring(4);

            // Declare a CourseDocument
            CourseDocument doc = new CourseDocument();

            XmlNodeList printItem = config.GetElementsByTagName("printItem");

            // Increment through the elements
            for (int i = 0; i < resourceElements.Count; i++)
            {
                // If the item is a content page
                if ("content".Equals(resourceElements[i].Attributes["d2l_2p0:material_type"].InnerText))
                {
                    // Get the path
                    filepath = resourceElements[i].Attributes["href"].InnerText;
                    // Increment through the items list
                    for (int h = 0; h < itemElements.Count; h++)
                    { 
                        // If the item matches its resource item, and the document is in fact an HTML pagem run!
                        if (itemElements[h].Attributes["identifierref"].InnerText.Equals(resourceElements[i].Attributes["identifier"].InnerText))
                        {
                            // Get the last set of data before running document
                            doctitle = itemElements[h].ChildNodes[0].InnerText;
                            ident = itemElements[h].Attributes["identifier"].InnerText;

                            // Get rid of commas
                            doctitle = doctitle.Replace(",", "");
                            doctitle = doctitle.Replace("\n", "");

                            // If said HTML file exists, run
                            if (File.Exists(Path.Combine(path, filepath)))
                            {
                                // Initialize the document
                                if (filepath.EndsWith("html"))
                                {
                                    doc.LoadDoc(Path.Combine(path, filepath), doctitle, orgUnitId, ident);

                                    // Start pulling data from the document and putting it into a return string
                                    foreach (XmlNode node in printItem)
                                    {
                                        type = node.SelectSingleNode("query").Attributes["type"].InnerText;
                                        switch (type)
                                        {
                                            case "OUI":
                                                reportString += orgUnitId + ",";// OrgUnitID of document
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
                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
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
            // Appends the content to the file. Creates the file if it doesn't already exist.
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

        /// <summary>
        /// Recursively deletes any extracted courses that parsed successfully
        /// </summary>
        /// <param name="parsedCourses"></param>
        public static void CleanUp(List<FileInfo> parsedCourses)
        {
            foreach (FileInfo parsedCourse in parsedCourses)
            {
                Directory.Delete(parsedCourse.FullName, true);
            }
        }
    }
}