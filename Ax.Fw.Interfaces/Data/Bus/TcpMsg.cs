using System;

namespace Ax.Fw.SharedTypes.Data.Bus;

public record TcpMsg(
  Guid Guid, 
  string TypeSlug, 
  byte[] Data);
