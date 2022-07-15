using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Ax.Fw.Tests.Tools;

public class RepeatAttribute : DataAttribute
{
    private readonly int p_count;

    public RepeatAttribute(int _count)
    {
        if (_count < 1)
            throw new ArgumentOutOfRangeException();

        p_count = _count;
    }

    public override IEnumerable<object[]> GetData(MethodInfo _testMethod)
    {
        for (var i = 0; i < p_count; i++)
            yield return new object[] { i };
    }
}