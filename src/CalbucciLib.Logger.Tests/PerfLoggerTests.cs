using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalbucciLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace CalbucciLib.Tests
{
    [TestClass()]
    public class PerfLoggerTests
    {
        [TestMethod()]
        public void JustBelowThreshold()
        {
            Logger customLogger = new Logger();
            bool logged = false;
            customLogger.AddExtension(logEvent =>
            {
                logged = true;
            });

            PerfLogger.DefaultLogger = customLogger;
            using (PerfLogger pl = new PerfLogger(TimeSpan.FromMilliseconds(100)))
            {
                Thread.Sleep(85);
            }

            Assert.IsFalse(logged);
        }

        [TestMethod()]
        public void JustAboveThreshold()
        {
            Logger customLogger = new Logger();
            bool logged = false;
            customLogger.AddExtension(logEvent =>
            {
                logged = true;
            });

            PerfLogger.DefaultLogger = customLogger;
            using (PerfLogger pl = new PerfLogger(TimeSpan.FromMilliseconds(50)))
            {
                Thread.Sleep(55);
            }

            Assert.IsTrue(logged);
        }
    }
}
