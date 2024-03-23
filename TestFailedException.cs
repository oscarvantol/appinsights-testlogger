using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppInsights.TestLogger
{
    internal class TestFailedException : Exception
    {
        public TestFailedException(TestResult testResult) : base(testResult.ErrorMessage)
        {
            this.Source = testResult.DisplayName;
        }
    }
}
