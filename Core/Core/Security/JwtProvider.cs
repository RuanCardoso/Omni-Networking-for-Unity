using System;
using System.Collections.Generic;
using System.Text;
using Omni.Shared;
using static Omni.Core.NetworkManager;

namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Specifies the available algorithms for computing the JSON Web Token (JWT) signature.
    /// </summary>
    public enum JwtAlgorithm
    {
        /// <summary>
        /// Specifies the HMAC using SHA256 algorithm.
        /// </summary>
        HS256,

        /// <summary>
        /// Specifies the HMAC using SHA384 algorithm.
        /// </summary>
        HS384,

        /// <summary>
        /// Specifies the HMAC using SHA512 algorithm.
        /// </summary>
        HS512,

        /// <summary>
        /// Specifies the RSA using SHA256 algorithm.
        /// </summary>
        RS256,

        /// <summary>
        /// Specifies the RSA using SHA384 algorithm.
        /// </summary>
        RS384,

        /// <summary>
        /// Specifies the RSA using SHA512 algorithm.
        /// </summary>
        RS512
    }

    /// <summary>
    /// Represents a JSON Web Token (JWT) and its properties.
    /// </summary>
    public partial class JwtToken
    {
        /// <summary>
        /// Gets or sets the payload of the JWT.
        /// </summary>
        public Dictionary<string, object> Payload { get; }

        /// <summary>
        /// Gets or sets the timestamp when the JWT was issued.
        /// </summary>
        public DateTime IssuedAt { get; }

        /// <summary>
        /// Gets or sets the timestamp when the JWT expires.
        /// </summary>
        public DateTime Expiration { get; }

        /// <summary>
        /// Gets or sets the issuer of the JWT.
        /// </summary>
        public string Issuer { get; }

        internal JwtToken(Dictionary<string, object> payload, DateTime issuedAt, DateTime expiration, string issuer)
        {
            Payload = payload;
            IssuedAt = issuedAt;
            Expiration = expiration;
            Issuer = issuer;
        }
    }

    // The class's methods must work for both sides, client and server.
    // the keys must be shared.

    /// <summary>
    /// Provides utility methods for generating and verifying JSON Web Tokens (JWTs).
    /// </summary>
    public static class JwtProvider
    {
        private static readonly Encoding _encoding = Encoding.UTF8;
        /// <summary>
        /// Computes a JSON Web Token (JWT) with the specified payload, expiration time, issuer, and algorithm.
        /// </summary>
        /// <param name="payload">The payload of the JWT. Defaults to a new empty dictionary.</param>
        /// <param name="expiration">The number of seconds until the JWT expires. Defaults to 86400 seconds (24 hours).</param>
        /// <param name="issuer">The issuer of the JWT. Defaults to an empty string.</param>
        /// <param name="algorithm">The algorithm to use for computing the JWT signature. Defaults to HS256.</param>
        /// <returns>A string representing the computed JWT.</returns>
        public static string Compute(Dictionary<string, object> payload = null, int expiration = 86400, string issuer = "", JwtAlgorithm algorithm = JwtAlgorithm.HS256) // 24 hours
        {
            try
            {
                var alg = algorithm.ToString();
                var isRSA = alg.StartsWith("RS");
                var header = new Dictionary<string, string>
                {
                    ["typ"] = "JWT",
                    ["alg"] = alg
                };

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var jwtPayload = new Dictionary<string, object>
                {
                    ["iss"] = issuer,
                    ["iat"] = now,
                    ["exp"] = now + expiration
                };

                if (payload != null)
                {
                    foreach (var item in payload)
                    {
                        if (item.Key == "iss" || item.Key == "iat" || item.Key == "exp")
                            throw new ArgumentException($"The key '{item.Key}' is reserved and cannot be used in the payload.");

                        jwtPayload[item.Key] = item.Value;
                    }
                }

                var headerJson = ToJson(header);
                var payloadJson = ToJson(jwtPayload);

                var headerEncoded = Base64UrlEncode(_encoding.GetBytes(headerJson));
                var payloadEncoded = Base64UrlEncode(_encoding.GetBytes(payloadJson));

                var dataToSign = $"{headerEncoded}.{payloadEncoded}";
                byte[] signature = isRSA
                    ? RsaProvider.Compute(_encoding.GetBytes(dataToSign), ServerSide.PEMPrivateKey, RsaKeyFormat.Pem, GetRsaHashAlgorithm(algorithm))
                    : HmacProvider.Compute(_encoding.GetBytes(dataToSign), SharedKey, GetHmacAlgorithm(algorithm));

                var signatureEncoded = Base64UrlEncode(signature);
                return $"{headerEncoded}.{payloadEncoded}.{signatureEncoded}";
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(ex.Message, NetworkLogger.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// Validates a JSON Web Token (JWT) with the specified issuer and algorithm.
        /// </summary>
        /// <param name="jwtToken">The JWT to validate.</param>
        /// <param name="token">The validated JWT token. This parameter is filled if the validation is successful.</param>
        /// <param name="issuer">The issuer of the JWT. Defaults to an empty string.</param>
        /// <param name="algorithm">The algorithm to use for validating the JWT signature. Defaults to HS256.</param>
        /// <returns>True if the JWT is valid; otherwise, false.</returns>
        public static bool Validate(string jwtToken, out JwtToken token, string issuer = "", JwtAlgorithm algorithm = JwtAlgorithm.HS256)
        {
            token = null;
            try
            {
                var parts = jwtToken.Split('.');
                if (parts.Length != 3)
                    return false;

                var headerJson = _encoding.GetString(Base64UrlDecode(parts[0]));
                var payloadJson = _encoding.GetString(Base64UrlDecode(parts[1]));
                var signature = Base64UrlDecode(parts[2]);

                var header = FromJson<Dictionary<string, string>>(headerJson);
                var jwtPayload = FromJson<Dictionary<string, object>>(payloadJson);

                var alg = header["alg"];
                var isRSA = alg.StartsWith("RS");
                if (header["typ"] != "JWT" || alg != algorithm.ToString())
                    return false;

                var dataToVerify = $"{parts[0]}.{parts[1]}";
                if (isRSA)
                {
                    if (!RsaProvider.Validate(_encoding.GetBytes(dataToVerify), signature, string.IsNullOrEmpty(ClientSide.PEMServerPublicKey) ? ServerSide.PEMPublicKey : ClientSide.PEMServerPublicKey, RsaKeyFormat.Pem, GetRsaHashAlgorithm(algorithm)))
                        return false;
                }
                else
                {
                    if (!HmacProvider.Validate(_encoding.GetBytes(dataToVerify), SharedKey, signature, GetHmacAlgorithm(algorithm)))
                        return false;
                }

                if (jwtPayload["iss"].ToString() != issuer)
                    return false;

                var exp = Convert.ToInt64(jwtPayload["exp"]);
                if (exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    return false;

                var payload = new Dictionary<string, object>();
                foreach (var item in jwtPayload)
                {
                    if (item.Key != "iss" && item.Key != "iat" && item.Key != "exp")
                        payload[item.Key] = item.Value;
                }

                var iat = Convert.ToInt64(jwtPayload["iat"]);
                if (iat > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    return false;

                token = new JwtToken(payload, DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime, DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime, issuer);
                return true;
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(ex.Message, NetworkLogger.LogType.Error);
                return false;
            }
        }

        private static RsaHashAlgorithm GetRsaHashAlgorithm(JwtAlgorithm algorithm)
        {
            return algorithm switch
            {
                JwtAlgorithm.RS256 => RsaHashAlgorithm.SHA256,
                JwtAlgorithm.RS384 => RsaHashAlgorithm.SHA384,
                JwtAlgorithm.RS512 => RsaHashAlgorithm.SHA512,
                _ => throw new ArgumentException("Unsupported JWT algorithm.")
            };
        }

        private static HmacAlgorithm GetHmacAlgorithm(JwtAlgorithm algorithm)
        {
            return algorithm switch
            {
                JwtAlgorithm.HS256 => HmacAlgorithm.SHA256,
                JwtAlgorithm.HS384 => HmacAlgorithm.SHA384,
                JwtAlgorithm.HS512 => HmacAlgorithm.SHA512,
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), "Unsupported JWT algorithm.")
            };
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string data)
        {
            switch (data.Length % 4)
            {
                case 2: data += "=="; break;
                case 3: data += "="; break;
            }

            return Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/'));
        }
    }
}