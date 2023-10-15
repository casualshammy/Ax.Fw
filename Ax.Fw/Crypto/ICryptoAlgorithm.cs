namespace Ax.Fw.Crypto;

public interface ICryptoAlgorithm
{
  byte[] Decrypt(byte[] _data);
  byte[] Encrypt(byte[] _data);
}