using System;

namespace Ax.Fw.SharedTypes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TcpMsgTypeAttribute : Attribute
{
  public TcpMsgTypeAttribute(string _typeSlug)
  {
    TypeSlug = _typeSlug;
  }

  public string TypeSlug { get; }

}
