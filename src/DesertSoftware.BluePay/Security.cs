//
//  Copyright 2013, Desert Software Solutions Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DesertSoftware.BluePay
{
    internal class Security
    {
        /// <summary>
        /// Creates a tamper proof seal using the default fields or TPS_DEF fields if specified.
        ///   (SECRET_KEY + ACCOUNT_ID + TRANS_TYPE + AMOUNT + MASTER_ID + NAME1 + PAYEMENT_ACCOUNT)
        /// </summary>
        /// <param name="secretKey">The secret key.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        static internal string CreateTamperProofSeal(string secretKey, NameValueCollection data) {
            if (data["TPS_DEF"] != null)
                return CreateTamperProofSeal(secretKey, data, data["TPS_DEF"]);

            return CreateTamperProofSeal(secretKey, data, "ACCOUNT_ID", "TRANS_TYPE", "AMOUNT", "MASTER_ID", "NAME1", "PAYMENT_ACCOUNT");
        }

        /// <summary>
        /// Creates a tamper proof seal using the specified fields.
        /// </summary>
        /// <param name="secretKey">The secret key.</param>
        /// <param name="data">The data.</param>
        /// <param name="fields">The data fields.</param>
        /// <returns></returns>
        static internal string CreateTamperProofSeal(string secretKey, NameValueCollection data, params string[] fields) {
            StringBuilder seal = new StringBuilder();

            // Computes an MD5 hash of the specified data field values
            using (var md5 = MD5.Create()) {
                var buffer = new StringBuilder();

                // Concatenate the data field values
                buffer.Append(secretKey);
                foreach (var name in fields) {
                    // Empty or missing fields are concatenated with an empty "" string value
                    buffer.Append(data[name] ?? "");
                }

                // compute the hash on the concatenated data field values
                byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(buffer.ToString()));

                // convert the hash to a hex formatted string
                for (var i = 0; i < hash.Length; i++)
                    seal.Append(hash[i].ToString("x2"));
            }

            return seal.ToString();
        }

        /// <summary>
        /// Creates a tamper proof seal using the fields described in the tpsDefinition.
        /// </summary>
        /// <param name="secretKey">The secret key.</param>
        /// <param name="data">The data.</param>
        /// <param name="tpsDefinition">The TPS definition (a space delimited list of data fields).</param>
        /// <returns></returns>
        static internal string CreateTamperProofSeal(string secretKey, NameValueCollection data, string tpsDefinition) {
            return CreateTamperProofSeal(secretKey, data, tpsDefinition.Split(' '));
        }

        /// <summary>
        /// Creates a TPS (Tamper Proof Seal) definition string suitable for a TPS_DEF field.
        /// </summary>
        /// <param name="fields">The fields to be included in the definition.</param>
        /// <returns></returns>
        static internal string CreateTPSDefinition(params string[] fields) {
            var definition = new StringBuilder();

            foreach (var name in fields)
                definition.AppendFormat(" {0}", name);

            return definition.ToString().Trim();
        }
    }
}