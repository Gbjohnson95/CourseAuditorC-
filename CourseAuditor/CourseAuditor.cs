using System;

namespace CourseAuditor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			CourseDocument test = new CourseDocument ();
			test.loadDoc ("C:\\Users\\gbjohnson\\Desktop\\test.html", "Test Document", "16094", "123456");
		}

		public void parseManifestAndRun( String resultname, String path ) {

		}
	}
}
