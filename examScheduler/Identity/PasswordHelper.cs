using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using System.Text;
using Util;

namespace examScheduler.Identity;

public static class PasswordHelper
{
	private const int TimeCost = 4;
	private const int MemoryCost = 1 << 16;
	private const int Lanes = 4;

	public static byte[ ] GenerateSalt(int length = 16)
	{
		var salt = new byte[ length ];
		RandomNumberGenerator.Fill(salt);
		return salt;
	}

	public static EncodedHash Hash(string password)
	{
		var config = new Argon2Config
		{
			Type = Argon2Type.DataIndependentAddressing,
			Version = Argon2Version.Nineteen,
			TimeCost = TimeCost,
			MemoryCost = MemoryCost,
			Lanes = Lanes,
			Threads = Environment.ProcessorCount,
			Salt = GenerateSalt(),
			HashLength = 32,
			Password = Encoding.UTF8.GetBytes(password),
		};

		using var argon2 = new Argon2(config);
		using (var hash = argon2.Hash())
		{
			return new(config.EncodeString(hash.Buffer));
		}
	}

	public static bool Verify(this EncodedHash encodedHash, string password)
	{
		return Argon2.Verify(encodedHash.Value, password);
	}
}

public record EncodedHash(string Value);