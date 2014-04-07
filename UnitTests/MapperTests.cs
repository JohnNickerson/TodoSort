using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class MapperTests
    {
        [Fact]
        public void Test_For_Colon_in_Tag()
        {
            // Create a test action item.
            ActionItem i = new ActionItem("testing", "A test with tag values containing colons (:)");
            i.Tags.Add("url", "http://user:pass@www.google.com/");

            // Serialise to disk.
            TodoTxtFileMapper m = new TodoTxtFileMapper("testActions.txt");
            m.Save(i);
            // Read from disk.
            var a = m.Load(i.ID);

            // Compare values.
            Assert.Equal(i.Tags.Count, a.Tags.Count);
            Assert.True(i.Tags.ContainsKey("url"));
            Assert.Equal(i.Tags["url"], a.Tags["url"]);

            // Remove file.
            if (System.IO.File.Exists("testActions.txt")) System.IO.File.Delete("testActions.txt");
        }
    }
}
