using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Net.Mail;

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
            var exception = new ArgumentNullException("argument");
            bool calledAppend = false;
            var logEvent = Logger.LogException(le =>
            {
                calledAppend = true;
            }, exception, "abc");

            Assert.IsTrue(calledAppend, "Didn't call appendData");
            Assert.AreEqual(logEvent.Type, "Exception");
            Assert.IsNotNull(logEvent.StackSignature);
            Assert.AreEqual(logEvent.Get("Exception", "Type"), "System.ArgumentNullException");
            
        }

        [TestMethod()]
        public void LogExceptionTest_NoLambda()
        {
            var exception = new ArgumentNullException("argument");
            var logEvent = Logger.LogException(exception, null);

            Assert.AreEqual(logEvent.Type, "Exception");
            Assert.IsNotNull(logEvent.StackSignature);
            Assert.AreEqual(logEvent.Get("Exception", "Type"), "System.ArgumentNullException");

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

		[TestMethod()]
		public void AddExtensionTest()
		{
			// TODO
		}

		[TestMethod()]
	    public void ShouldLogCallbackTest()
	    {
		    // TODO
	    }

	    [TestMethod]
	    public void SendEmailTest()
	    {
            Logger.Default.SendToEmailAddressFatal = "username@gmail.com";
            SmtpClient outlookserver = new SmtpClient("smtp.live.com")
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("username", "password"),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = 587,
                EnableSsl = true,
            };

            Logger.Default.SmtpClient = outlookserver;
            
            var LogEvent = Logger.LogFatal("Fatal error test with email", "abc");

            Assert.AreEqual(Logger.Default.ConfirmEmailSent, true);
            
        }
    }
}
