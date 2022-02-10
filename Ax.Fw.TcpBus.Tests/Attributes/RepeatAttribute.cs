using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ax.Fw.TcpBus.Tests.Attributes
{
    public class RepeatAttribute : Xunit.Sdk.DataAttribute
    {
        private readonly int p_count;

        public RepeatAttribute(int _count)
        {
            if (_count < 1)
            {
                throw new ArgumentOutOfRangeException(
                   nameof(_count),
                   "Repeat count must be greater than 0."
                );
            }
            p_count = _count;
        }

        public override IEnumerable<object[]> GetData(MethodInfo _testMethod)
        {
            for (var i = 0; i < p_count; i++)
                yield return new object[] { i };
        }
    }

}
