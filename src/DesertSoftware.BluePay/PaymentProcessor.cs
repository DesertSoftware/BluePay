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
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace DesertSoftware.BluePay
{
    /// <summary>
    /// Payment processor implementation based on BluePay 2.0 specs.
    /// https://secure.assurebuy.com/BluePay/BluePay_bp20post/Bluepay20post.txt
    /// </summary>
    public class PaymentProcessor
    {
        private const string DEFAULT_GATEWAY_URL = "https://secure.bluepay.com/interfaces/bp20post";

        public string AccountId { get; set; }
        public string SecretKey { get; set; }
        public string GatewayUrl { get; set; }

        public PaymentProcessor(string accountId, string secretKey, string gatewayUrl = DEFAULT_GATEWAY_URL) {
            AccountId = accountId;
            SecretKey = secretKey;
            GatewayUrl = gatewayUrl;
        }

        public NameValueCollection ProcessPayment(NameValueCollection paymentData) {
            string result;
            var resultData = new NameValueCollection();

            // Create tamper proof seal
            if (paymentData["TAMPER_PROOF_SEAL"] == null)
                paymentData.Add("TAMPER_PROOF_SEAL", Security.CreateTamperProofSeal(SecretKey, paymentData));

            // Post data to gateway
            try {
                using (WebClient client = new WebClient()) {

                    client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    result = Encoding.ASCII.GetString(client.UploadValues(GatewayUrl, "POST", paymentData));
                }
            } catch (WebException wx) {

                // Bad transactions are reported as http 400 Bad Request exceptions
                // The actual details are contained in the response stream
                using (System.IO.StreamReader reader = new System.IO.StreamReader(wx.Response.GetResponseStream())) {
                    result = reader.ReadToEnd();
                    reader.Close();
                }
            } catch (Exception ex) {

                // Something unexpected happened. Capture the message
                result = string.Format("STATUS=E&MESSAGE={0}", ex.Message);
            }

            resultData.Add("RAW_RESULT", result);

            // parse the result into a name value collection
            foreach (var namevalue in HttpUtility.UrlDecode(result).Split('&')) {
                string[] fields = namevalue.Split('=');

                if (fields.Length > 1)
                    resultData.Add(fields[0], fields[1]);
            }

            return resultData;
        }

        /// <summary>
        /// Processes the credit card sale.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="cardNumber">The card number.</param>
        /// <param name="CVV2">The CV v2.</param>
        /// <param name="expiration">The expiration date of the card in MMYY.</param>
        /// <param name="testMode">if set to <c>true</c> use TEST mode.</param>
        /// <param name="optionalData">Additional optional information such as NAME1.</param>
        /// <returns></returns>
        public NameValueCollection ProcessCreditCardSale(
            double amount, string cardNumber, string CVV2, string expiration, bool testMode = false, NameValueCollection optionalData = null) {

            var data = new NameValueCollection();

            data.Add("ACCOUNT_ID", AccountId);
            data.Add("TRANS_TYPE", "SALE");
            data.Add("PAYMENT_TYPE", "CREDIT");
            data.Add("MODE", testMode ? "TEST" : "LIVE");

            // credit card required fields
            data.Add("AMOUNT", amount.ToString());
            data.Add("PAYMENT_ACCOUNT", cardNumber);
            data.Add("CARD_CVV2", CVV2);
            data.Add("CARD_EXPIRE", expiration);

            // Optional payment data fields
            if (optionalData != null) {
                foreach (var name in optionalData.AllKeys)
                    data.Add(name, optionalData[name]);
            }

            return ProcessPayment(data);
        }

        /// <summary>
        /// Processes the credit card sale.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="cardNumber">The card number.</param>
        /// <param name="CVV2">The CV v2.</param>
        /// <param name="expiration">The expiration of the card in MMYY.</param>
        /// <param name="testMode">if set to <c>true</c> use TEST mode. Optional, default = false</param>
        /// <param name="firstName">The first name. Optional</param>
        /// <param name="lastName">The last name. Optional</param>
        /// <param name="addressLine1">The address line1. Optional</param>
        /// <param name="addressLine2">The address line2. Optional</param>
        /// <param name="city">The city. Optional</param>
        /// <param name="state">The state. Optional</param>
        /// <param name="zipCode">The zip code. Optional</param>
        /// <param name="country">The country. Optional</param>
        /// <param name="companyName">Name of the company. Optional</param>
        /// <param name="email">The email address. Optional</param>
        /// <param name="phone">The phone number. Optional</param>
        /// <param name="memo">The memo describing this transaction that will appear on the statement. Optional 128 character max</param>
        /// <param name="customId1">An internal reference value. Optional 16 character max</param>
        /// <param name="customId2">An internal reference value.  Optional 64 character max</param>
        /// <param name="taxAmount">The tax amount. Optional</param>
        /// <param name="miscAmount">The misc amount. Optional</param>
        /// <returns></returns>
        public NameValueCollection ProcessCreditCardSale(
            double amount, string cardNumber, string CVV2, string expiration,
            bool testMode = false, string firstName = "", string lastName = "", string addressLine1 = "", string addressLine2 = "", string city = "",
            string state = "", string zipCode = "", string country = "", string companyName = "", string email = "", string phone = "",
            string memo = "", string customId1 = "", string customId2 = "", double taxAmount = 0, double miscAmount = 0) {

            var data = new NameValueCollection();

            data.Add("ACCOUNT_ID", AccountId);
            data.Add("TRANS_TYPE", "SALE");
            data.Add("PAYMENT_TYPE", "CREDIT");
            data.Add("MODE", testMode ? "TEST" : "LIVE");

            // credit card required fields
            data.Add("AMOUNT", amount.ToString());
            data.Add("PAYMENT_ACCOUNT", cardNumber);
            data.Add("CARD_CVV2", CVV2);
            data.Add("CARD_EXPIRE", expiration);

            // Optional merchant data fields
            data.Add("COMPANY_NAME", companyName);
            data.Add("NAME1", firstName);
            data.Add("NAME2", lastName);
            data.Add("ADDR1", addressLine1);
            data.Add("ADDR2", addressLine2);
            data.Add("CITY", city);
            data.Add("STATE", state);
            data.Add("ZIP", zipCode);
            data.Add("COUNTRY", country);
            data.Add("EMAIL", email);
            data.Add("PHONE", phone);
            data.Add("MEMO", memo);
            data.Add("CUSTOM_ID1", customId1);
            data.Add("CUSTOM_ID2", customId2);
            data.Add("AMOUNT_TAX", taxAmount.ToString());
            data.Add("AMOUNT_MISC", miscAmount.ToString());

            return ProcessPayment(data);
        }
    }
}
