using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalbucciLib;
using CalbucciLib.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CalbucciLib.Tests
{
    [TestClass()]
    public class LogEventTests
    {
        [TestMethod()]
        public void AddTest()
        {
            LogEvent logEvent = new LogEvent("Error");
            string originalName = "Marcelo Calbucci";
            logEvent.Add("UserData", "Name", originalName);

            string name = logEvent.Get("UserData", "Name") as string;

            Assert.AreEqual(name, originalName);
        }


        [TestMethod()]
        public void ToJsonTest()
        {

            var logEvent = new LogEvent("Error");
            logEvent.Message = "Error message";
            logEvent.StackSignature = "1234";

            var catTest = logEvent.GetOrCreateCategory("Test");
            catTest["a"] = "abc";
            catTest["b"] = 27;
            catTest["c"] = 13.1;
            catTest["d"] = new List<string>() {"abc", "def"};
            catTest["e"] = new DateTime(2015, 3, 14, 9, 26, 53);

            string json = logEvent.ToJson(true);

            var logEvent2 = LogEvent.FromJson(json);

            Assert.AreEqual(logEvent.UniqueId, logEvent2.UniqueId);
            Assert.AreEqual(logEvent.Type, logEvent2.Type);
            Assert.AreEqual(logEvent.EventDate, logEvent2.EventDate);
            Assert.AreEqual(logEvent.EventDateUtc, logEvent2.EventDateUtc);
            Assert.AreEqual(logEvent.Message, logEvent2.Message);
            Assert.AreEqual(logEvent.StackSignature, logEvent2.StackSignature);

            var cat2 = logEvent2.GetCategory("Test");

            Assert.IsTrue(CompareUtils.AreEqual(catTest, cat2));
        }

        [TestMethod()]
        public void HtmlifyTest()
        {
            var logEvent = new LogEvent("Error");
            logEvent.Message = "Error message";
            logEvent.StackSignature = "1234";

            var catTest = logEvent.GetOrCreateCategory("Test");
            catTest["a"] = "abc";
            catTest["b"] = 27;
            catTest["c"] = 13.1;
            catTest["d"] = new List<string>() { "abc", "def" };
            catTest["e"] = new DateTime(2015, 3, 14, 9, 26, 53);

            string html = logEvent.Htmlify();

            Assert.IsNotNull(html);
        }

        

        
    }
}
