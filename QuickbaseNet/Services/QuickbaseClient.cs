﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuickbaseNet.Errors;
using QuickbaseNet.Requests;
using QuickbaseNet.Responses;

namespace QuickbaseNet.Services
{
    public class QuickbaseClient
    {
        private const string BaseUrl = "https://api.quickbase.com";
        private const string UserAgent = "QuickbaseNet/0.1.1";

        public HttpClient Client { get; set; } = new HttpClient();

        public QuickbaseClient(string realm, string userToken)
        {
            Client.BaseAddress = new Uri(BaseUrl);
            Client.DefaultRequestHeaders.Add("QB-Realm-Hostname", $"{realm}.quickbase.com");
            Client.DefaultRequestHeaders.Add("Authorization", $"QB-USER-TOKEN {userToken}");
            Client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public async Task<QuickbaseResult<QuickbaseQueryResponse>> QueryRecords(QuickbaseQueryRequest quickBaseRequest)
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(quickBaseRequest), Encoding.UTF8, "application/json");

            var response = await Client.PostAsync("/v1/records/query", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var queryResponse = JsonConvert.DeserializeObject<QuickbaseQueryResponse>(jsonResponse);

                // Check if data is null or empty
                return queryResponse.Data.Count == 0
                    ? QuickbaseResult.Failure<QuickbaseQueryResponse>(QuickbaseError.NotFound("QuickbaseNet.Failure",
                        "No records found", $"The query did not find any records matching that criteria"))
                    : QuickbaseResult.Success(queryResponse);
            }

            var errorResponse = JsonConvert.DeserializeObject<QuickbaseErrorResponse>(jsonResponse);

            // Check if its 4xx error
            if (response.StatusCode >= System.Net.HttpStatusCode.BadRequest &&
                response.StatusCode < System.Net.HttpStatusCode.InternalServerError)
            {
                return QuickbaseResult.Failure<QuickbaseQueryResponse>(QuickbaseError.ClientError("QuickbaseNet.ClientError", errorResponse.Message, errorResponse.Description));
            }

            // Check if its 5xx error
            if (response.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            {
                return QuickbaseResult.Failure<QuickbaseQueryResponse>(QuickbaseError.ServerError("QuickbaseNet.ServerError", errorResponse.Message, errorResponse.Description));
            }

            // Return generic failure
            return QuickbaseResult.Failure<QuickbaseQueryResponse>(QuickbaseError.Failure("QuickbaseNet.Failure", errorResponse.Message, errorResponse.Description));
        }

        internal async Task<(QuickbaseRecordUpdateResponse Response, QuickbaseErrorResponse Error, bool IsSuccess)>
            InsertRecords(InsertOrUpdateRecordRequest quickBaseRequest)
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(quickBaseRequest), Encoding.UTF8, "application/json");

            var response = await Client.PostAsync("/v1/records", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return (JsonConvert.DeserializeObject<QuickbaseRecordUpdateResponse>(jsonResponse), null, true);
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            return (null, JsonConvert.DeserializeObject<QuickbaseErrorResponse>(errorResponse), false);
        }

        internal async Task<(QuickbaseRecordUpdateResponse Response, QuickbaseErrorResponse Error, bool IsSuccess)>
            UpdateRecords(InsertOrUpdateRecordRequest quickBaseRequest)
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(quickBaseRequest), Encoding.UTF8, "application/json");

            var response = await Client.PostAsync("/v1/records", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return (JsonConvert.DeserializeObject<QuickbaseRecordUpdateResponse>(jsonResponse), null, true);
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            return (null, JsonConvert.DeserializeObject<QuickbaseErrorResponse>(errorResponse), false);
        }
    }
}
