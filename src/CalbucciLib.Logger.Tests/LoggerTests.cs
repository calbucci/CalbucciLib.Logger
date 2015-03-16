using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using CalbucciLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalbucciLib.Tests
{
    [TestClass()]
	public class LoggerTests
    {
        [TestMethod()]
        public void LogErrorTest()
        {
            bool calledAppend = false;
            var logEvent = Logger.LogError(le =>
            {
                calledAppend = true;
            }, "This is an error {0}", "abc");
            
            Assert.IsTrue(calledAppend, "Didn't call appendData");
            Assert.AreEqual(logEvent.Type, "Error");
            Assert.AreEqual(logEvent.Message, "This is an error abc");
            Assert.IsNotNull(logEvent.StackSignature);
        }

        [TestMethod()]
        public void LogExceptionTest()
        {
            var exception = new ArgumentNullException("The argument is null");
            bool calledAppend = false;
            var logEvent = Logger.LogException(le =>
            {
                calledAppend = true;
            }, exception, "This is an exception {0}", "abc");

            Assert.IsTrue(calledAppend, "Didn't call appendData");
            Assert.AreEqual(logEvent.Type, "Exception");
            Assert.AreEqual(logEvent.Message, "This is an exception abc");
            Assert.IsNotNull(logEvent.StackSignature);
            Assert.AreEqual(logEvent.Get("Exception", "Type"), "System.ArgumentNullException");
            
        }

        [TestMethod()]
        public void EmailTest()
        {
            var customLogger = new Logger();
            customLogger.SendToEmailAddress = "marcelo@calbucci.com";

            customLogger.Error("This is an error");

        }

		[TestMethod()]
		public void LogWarningTest()
		{
			HttpContext.Current = new HttpContext(new HttpRequest("", "http://tempuri.org", ""),
					new HttpResponse(new StringWriter()));

			string userName = "John Doe";
			var context = new HttpContext(new HttpRequest(null, "http://blog.calbucci.com", null), new HttpResponse(null));
			context.User = Mock.Of<ClaimsPrincipal>(cp =>
				cp.Identity.IsAuthenticated == true &&
				cp.Identity.Name == userName
				);

			HttpContext.Current = context;
			
			var logEvent = Logger.LogWarning("Warning 1", "abc");

			Assert.AreEqual(logEvent.Get("HttpUser", "Name"), userName);
		}
    }
}
