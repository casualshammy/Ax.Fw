namespace Ax.Fw.SharedTypes.Interfaces;

public interface ICryptoAlgorithm
{
  byte[] Decrypt(byte[] _data);
  byte[] Encrypt(byte[] _data);
}