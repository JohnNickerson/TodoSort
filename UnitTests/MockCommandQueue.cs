using System;
using System.Collections.Generic;
using AssimilationSoftware.Maroon.Commands;

namespace AssimilationSoftware.TodoSort.UnitTests
{
    internal class MockCommandQueue : Maroon.Interfaces.ICommandListMapper
    {
        public MockCommandQueue()
        {
        }

        public IEnumerable<Command> Commands
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(Command cmd)
        {
            throw new NotImplementedException();
        }

        public void Read()
        {
            throw new NotImplementedException();
        }

        public void Write()
        {
            throw new NotImplementedException();
        }
    }
}