using OtpNet;

namespace SanShine.SaaS.Core.Application.Services;

public interface IMfaService
{
	string GenerateSecretKey();
	string GenerateTOTP(string secretKey, int timeStep = 30);
	bool ValidateTOTP(string secretKey, string code, int timeStep);

}
public class MfaService : IMfaService
{
	private const int DefaultTimeStep = 90; // Varsayılan zaman dilimi 30 saniye

	// Secret key oluşturma metodu
	public string GenerateSecretKey()
	{
		var secretKey = KeyGeneration.GenerateRandomKey(20); // 20 karakterlik rastgele bir key oluştur.
		return Base32Encoding.ToString(secretKey);
	}

	// TOTP kodu üretme metodu
	public string GenerateTOTP(string secretKey, int timeStep = DefaultTimeStep)
	{
		var otp = new Totp(Base32Encoding.ToBytes(secretKey), timeStep);
		return otp.ComputeTotp();
	}

	// TOTP kodunu doğrulama metodu
	public bool ValidateTOTP(string secretKey, string code, int timeStep = DefaultTimeStep)
	{
		var otp = new Totp(Base32Encoding.ToBytes(secretKey), timeStep);

		// VerificationWindow ile 1 önceki ve 1 sonraki timeStep'leri kontrol ediyoruz.
		return otp.VerifyTotp(code, out long timeWindowUsed, new VerificationWindow(previous: 1, future: 1));
	}
}
