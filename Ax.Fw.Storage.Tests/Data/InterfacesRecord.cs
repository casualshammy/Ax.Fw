namespace Ax.Fw.Storage.Tests.Data;

internal record InterfacesRecord(
  IReadOnlyList<int> ListOfInt, 
  IReadOnlyDictionary<string, int> Dictionary, 
  RecordEnum Enum);
