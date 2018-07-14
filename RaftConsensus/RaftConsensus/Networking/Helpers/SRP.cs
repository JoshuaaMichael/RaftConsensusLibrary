/*
 * Project: Eneter.SecureRemotePassword
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2016
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
*/


using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Eneter.SecureRemotePassword
{
    /// <summary>
    /// Sercure Remote Password (SRP-6a).
    /// </summary>
    /// <remarks>
    /// Provides functionality for implementing Secure Remote Password protocol.
    /// Names of methods match the naming convention which is defined in the protocol specification:<br/>
    /// http://srp.stanford.edu/design.html
    /// 
    /// Here is the summary of SRP parameters:
    /// N       A large safe prime(N = 2q+1, where q is prime)
    ///         All arithmetic is done modulo N.
    /// g       A generator modulo N
    /// k       Multiplier parameter
    /// s       User's salt
    /// I       Username
    /// p       Cleartext Password
    /// H()     One-way hash function
    /// ^       (Modular) Exponentiation
    /// u       Random scrambling parameter
    /// a, b    Secret ephemeral values
    /// A, B    Public ephemeral values
    /// x       Private key(derived from p and s)
    /// v       Password verifier
    /// </remarks>
    public static class SRP
    {
        private static RNGCryptoServiceProvider myRandomGenerator = new RNGCryptoServiceProvider();

        // The prime number is:
        // 17520593701874158028370915808709757311559135004731036178287990173853
        // 33366544841483884826147664648546626630359228048030338134283071703680
        // 28802332669752540042735820584582207684890049393260621695533226569112
        // 16759726125778565962920386496090139562747608067026106900488218998719
        // 91953917497602225116280480886924497108570268941426816646936938460102
        // 8647069420217148751652732063899795068604670043
        private static BigInteger N = new BigInteger(
            new byte[] { 91, 96, 240, 97, 97, 104, 3, 80, 79, 11, 90, 124, 155, 79, 2, 156, 122, 6, 231, 12, 212, 140, 149, 199, 217, 27, 124, 210, 127, 24, 4, 234, 99, 177, 73, 32, 187, 20, 235, 157, 132, 235, 69, 126, 82, 194, 236, 201, 0, 113, 216, 166, 42, 75, 91, 225, 13, 224, 101, 31, 188, 148, 8, 105, 145, 24, 222, 28, 75, 116, 222, 192, 95, 15, 172, 152, 156, 58, 189, 144, 190, 11, 165, 135, 215, 90, 217, 55, 24, 212, 128, 103, 133, 206, 95, 108, 120, 75, 0, 110, 129, 206, 22, 50, 40, 17, 17, 72, 220, 235, 66, 210, 185, 4, 223, 199, 174, 190, 149, 59, 63, 164, 182, 167, 6, 252, 114, 56, 196, 13, 105, 125, 24, 177, 10, 9, 42, 103, 140, 115, 93, 87, 231, 168, 67, 120, 10, 106, 213, 1, 148, 179, 20, 25, 129, 110, 130, 70, 121, 215, 0 });
        private static int myPrimeNumberSize = N.ToByteArray().Length;
        private static BigInteger g = 2;
        private static BigInteger k = H(N, g);


        /// <summary>
        /// Allows to setup a different large prime number.
        /// </summary>
        /// <param name="largePrimeNumber"></param>
        public static void CustomInit(byte[] largePrimeNumber)
        {
            N = new BigInteger(largePrimeNumber);
            myPrimeNumberSize = largePrimeNumber.Length;
            k = H(N, g);
        }

        /// <summary>
        /// Generates 16 byte salt.
        /// </summary>
        /// <returns>salt 's'</returns>
        public static byte[] s()
        {
            byte[] aSalt = GetPositiveRandomNumber(16);
            return aSalt;
        }

        /// <summary>
        /// Calculates user private key 'x' from user-password and salt.
        /// </summary>
        /// <remarks>
        /// Note that this key is not supposed to be stored. Database of users should store only salt 's' and verifier 'v'.
        /// </remarks>
        /// <param name="password">text of user password 'p'</param>
        /// <param name="sBytes">salt 's'</param>
        /// <returns>user private key 'x'</returns>
        public static byte[] x(string password, byte[] sBytes)
        {
            byte[] aPasswordBytes = Encoding.UTF8.GetBytes(password);

            // x = H(s, p)
            byte[] xBytes = H(sBytes, aPasswordBytes);
            return xBytes;
        }

        /// <summary>
        /// Calculates verifier 'v' from the user private key 'x'.
        /// </summary>
        /// <param name="xBytes">user private key 'x'</param>
        /// <returns>verifier 'v'</returns>
        public static byte[] v(byte[] xBytes)
        {
            BigInteger x = new BigInteger(xBytes);

            // v = g^x % N
            BigInteger v = BigInteger.ModPow(g, x, N);
            return v.ToByteArray();
        }


        /// <summary>
        /// Generates secret ephemeral value 'a' for client.
        /// </summary>
        /// <returns>client secret ephemeral value 'a'</returns>
        public static byte[] a()
        {
            return GetSecretEphemeralValue();
        }

        /// <summary>
        /// Calculates public ephemeral value 'A' for client.
        /// </summary>
        /// <param name="aBytes">client secret ephemeral value 'a'</param>
        /// <returns>client public ephemeral value 'A'</returns>
        public static byte[] A(byte[] aBytes)
        {
            BigInteger a = new BigInteger(aBytes);

            // A = g^a % N
            BigInteger A = BigInteger.ModPow(g, a, N);
            return A.ToByteArray();
        }

        /// <summary>
        /// Used by the service to check public ephemeral value 'A' from the client.
        /// </summary>
        /// <param name="ABytes">client public ephemeral value</param>
        /// <returns>true if the value is ok</returns>
        public static bool IsValid_A(byte[] ABytes)
        {
            BigInteger a = new BigInteger(ABytes);
            return a % N != 0;
        }

        /// <summary>
        /// Generates secret ephemeral value 'b' for service.
        /// </summary>
        /// <returns>service secret ephemeral value 'b'</returns>
        public static byte[] b()
        {
            return GetSecretEphemeralValue();
        }

        /// <summary>
        /// Calculates public ephemeral value 'B' for service.
        /// </summary>
        /// <param name="bBytes">service secret ephemeral value 'b'</param>
        /// <param name="vBytes">verifier 'v'</param>
        /// <returns>service public ephemeral value 'B'</returns>
        public static byte[] B(byte[] bBytes, byte[] vBytes)
        {
            BigInteger b = new BigInteger(bBytes);
            BigInteger v = new BigInteger(vBytes);

            // B = (k * v + (g^b % N)) % N
            BigInteger B = (k * v + BigInteger.ModPow(g, b, N)) % N;
            return B.ToByteArray();
        }

        /// <summary>
        /// Calculates random scrambling parameter.
        /// </summary>
        /// <param name="ABytes">client public ephemeral value 'A'</param>
        /// <param name="BBytes">service public ephemeral value 'B'</param>
        /// <returns>random scrambling parameter</returns>
        public static byte[] u(byte[] ABytes, byte[] BBytes)
        {
            // u = H(A, B)
            byte[] uBytes = H(ABytes, BBytes);
            return uBytes;
        }

        /// <summary>
        /// Used by the client to check public ephemeral value 'B' from the client
        /// and to check calculated scrambling parameter.
        /// </summary>
        /// <param name="BBytes">service public ephemeral value 'B</param>
        /// <param name="uBytes">random scrambling parameter 'u'</param>
        /// <returns></returns>
        public static bool IsValid_B_u(byte[] BBytes, byte[] uBytes)
        {
            BigInteger B = new BigInteger(BBytes);
            BigInteger u = new BigInteger(uBytes);
            return B % N != 0 && u != 0;
        }

        /// <summary>
        /// Calculates session key 'K' for the client.
        /// </summary>
        /// <param name="BBytes">service public ephemeral value 'B'</param>
        /// <param name="xBytes">user private key 'x'</param>
        /// <param name="uBytes">random scrambling parameter</param>
        /// <param name="aBytes">client secret ephemeral value 'a'</param>
        /// <returns>session key 'K'</returns>
        public static byte[] K_Client(byte[] BBytes, byte[] xBytes, byte[] uBytes, byte[] aBytes)
        {
            BigInteger B = new BigInteger(BBytes);
            BigInteger x = new BigInteger(xBytes);
            BigInteger u = new BigInteger(uBytes);
            BigInteger a = new BigInteger(aBytes);

            // S = (B − k * (g^x % N))^(a + u * x) % N
            BigInteger aBase = B - k * BigInteger.ModPow(g, x, N);
            BigInteger S = BigInteger.ModPow(aBase, a + u * x, N);
            if (S < 0)
            {
                // The problem is that C# incorrectly calculates modulus of negative numbers.
                // E.g. -2 % 5 = -2 but the correct result is 3.
                // Because of that the formula: S = [aBase ^ (a + u * x)] % N can give an incorrect result.
                // Therefore it is needed to recalculate it.
                S = S + N;
            }

            // K = H(S)
            BigInteger K = H(S);
            return K.ToByteArray();
        }

        /// <summary>
        /// Calculates session key 'K' for service.
        /// </summary>
        /// <param name="ABytes">client public ephemeral value 'A'</param>
        /// <param name="vBytes">verifier 'v'</param>
        /// <param name="uBytes">random scrambling parameter</param>
        /// <param name="bBytes">service secret ephemeral value 'b'</param>
        /// <returns>session key 'K'</returns>
        public static byte[] K_Service(byte[] ABytes, byte[] vBytes, byte[] uBytes, byte[] bBytes)
        {
            BigInteger A = new BigInteger(ABytes);
            BigInteger v = new BigInteger(vBytes);
            BigInteger u = new BigInteger(uBytes);
            BigInteger b = new BigInteger(bBytes);

            // S = (A * (v^u % N))^b % N
            BigInteger S = BigInteger.ModPow(A * BigInteger.ModPow(v, u, N), b, N);

            // K = H(S)
            BigInteger K = H(S);
            return K.ToByteArray();
        }

        /// <summary>
        /// Calculates evidence message 'M1' which can be sent from client to service.
        /// </summary>
        /// <param name="ABytes">client public ephemeral value 'A'</param>
        /// <param name="BBytes">service public ephemeral value 'B'</param>
        /// <param name="KBytes">session key 'K'</param>
        /// <returns>evidence message</returns>
        public static byte[] M1(byte[] ABytes, byte[] BBytes, byte[] KBytes)
        {
            return H(ABytes, BBytes, KBytes);
        }

        /// <summary>
        /// Calculates evidence message 'M2' which can be sent from service to client.
        /// </summary>
        /// <param name="ABytes">client public ephemeral value 'A'</param>
        /// <param name="M1Bytes">evidence message 'M1' received from the client</param>
        /// <param name="KBytes">session key 'K'</param>
        /// <returns>evidence message</returns>
        public static byte[] M2(byte[] ABytes, byte[] M1Bytes, byte[] KBytes)
        {
            return H(ABytes, M1Bytes, KBytes);
        }

        private static BigInteger H(params BigInteger[] values)
        {
            byte[][] aParams = values.Select(x => x.ToByteArray()).ToArray();
            byte[] aHash = H(aParams);
            return new BigInteger(aHash);
        }

        private static byte[] H(params byte[][] values)
        {
            byte[] aBytes;

            if (values.Length == 1)
            {
                aBytes = values[0];
            }
            else
            {
                int aSize = values.Sum(x => x.Length);
                aBytes = new byte[aSize];
                for (int i = 0; i < values.Length; ++i)
                {
                    int aDstOffset = (i == 0) ? 0 : values[i - 1].Length;
                    Buffer.BlockCopy(values[i], 0, aBytes, aDstOffset, values[i].Length);
                }
            }

            // Compute the hash.
            SHA256Managed aSha = new SHA256Managed();

            // Concatenate 0 to ensure the generated hash number will be understood as a positive number.
            byte[] aHash = aSha.ComputeHash(aBytes).Concat(new byte[] { 0 }).ToArray();
            return aHash;
        }

        // Gets random value between 0 and N.
        private static byte[] GetSecretEphemeralValue()
        {
            BigInteger aValue;
            do
            {
                byte[] aValueBytes = GetPositiveRandomNumber(myPrimeNumberSize);
                aValue = new BigInteger(aValueBytes);
            } while (aValue >= N);

            return aValue.ToByteArray();
        }

        private static byte[] GetPositiveRandomNumber(int length)
        {
            byte[] aValue = new byte[length];
            myRandomGenerator.GetBytes(aValue);

            // Set sign bit to positive.
            aValue[aValue.Length - 1] &= 0x7F;

            return aValue;
        }
    }
}
