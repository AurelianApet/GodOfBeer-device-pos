using GodOfBeer.util;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;

namespace GodOfBeer.restful
{
    public class ApiClient : GenericSingleton<ApiClient>
    {
        public class ApiInfo
        {
            public string api { get; set; }
            public object resObject { get; set; }
        }
        public class ApiResponse
        {
            public int? suc { get; set; }
            public string msg { get; set; }
            public Dictionary<string, object> dataMap { get; set; }
        }

        Dictionary<Type, ApiInfo> matchDic = null;

        JsonSerializer json = new JsonSerializer();

        public class LoginAPI
        {
            public string userID { get; set; }
            public string password { get; set; }
            public int? usertype { get; set; }
        }

        public class PosExeSuccessApi
        {
            public int? pub_id { get; set; }
        }

        public class PosExeDisconnectApi
        {
            public int? pub_id { get; set; }
        }

        public class FindTapApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
        }

        public class GetTagInfoApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public string tag_data { get; set; }
        }

        public class GetTapProductInfoApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
        }

        public class SetDeviceInfoApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public int? is_keg_reset { get; set; }
            public int? is_valve_error { get; set; }
            public int? is_soldout { get; set; }
            public int? status { get; set; }
            public int? is_valve_on { get; set; }
        }

        public class GetOrderNumberApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }

            public string tag_data { get; set; }
        }

        public class SetOrderNumberApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public int last_order_number { get; set; }
        }

        public class CreateOrderApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public int? order_number { get; set; }
            public int? product_id { get; set; }
            public string tag_data { get; set; }
            public int? using_amount { get; set; }
            public int? is_soldout { get; set; }
        }

        public class GetProductQuantityApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
        }

        public class SetTapPulseApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public int? pulse { get; set; }
        }

        public class SensorNotWorkApi
        {
            public int? pub_id { get; set; }
            public string serial_number { get; set; }
            public string tag_data { get; set; }
        }

        public ApiClient()
        {
            matchDic = new Dictionary<Type, ApiInfo>();
            matchDic.Add(typeof(LoginAPI), new ApiInfo() { api = "login", resObject = new ApiResponse() });
            matchDic.Add(typeof(FindTapApi), new ApiInfo() { api = "find-tap", resObject = new ApiResponse() });
            matchDic.Add(typeof(GetTagInfoApi), new ApiInfo() { api = "get-tag-info", resObject = new ApiResponse() });
            matchDic.Add(typeof(GetTapProductInfoApi), new ApiInfo() { api = "get-tap-productinfo", resObject = new ApiResponse() });
            matchDic.Add(typeof(SetDeviceInfoApi), new ApiInfo() { api = "set-device-info", resObject = new ApiResponse() });
            matchDic.Add(typeof(GetOrderNumberApi), new ApiInfo() { api = "get-ordernumber", resObject = new ApiResponse() });
            matchDic.Add(typeof(SetOrderNumberApi), new ApiInfo() { api = "set-ordernumber", resObject = new ApiResponse() });
            matchDic.Add(typeof(CreateOrderApi), new ApiInfo() { api = "self-order", resObject = new ApiResponse() });
            matchDic.Add(typeof(GetProductQuantityApi), new ApiInfo() { api = "get-product-quantity", resObject = new ApiResponse() });
            matchDic.Add(typeof(SetTapPulseApi), new ApiInfo() { api = "set-tap-pulse", resObject = new ApiResponse() });
            matchDic.Add(typeof(PosExeSuccessApi), new ApiInfo() { api = "set-existexe-success", resObject = new ApiResponse() });
            matchDic.Add(typeof(PosExeDisconnectApi), new ApiInfo() { api = "existexe-disconnect", resObject = new ApiResponse() });
            matchDic.Add(typeof(SensorNotWorkApi), new ApiInfo() { api = "sensor-notwork", resObject = new ApiResponse() });
        }

        public ApiResponse PostQuery(object postData)
        {
            ApiResponse result = null;
            try
            {
                var client = new RestClient(ConfigSetting.api_server_domain);
                var request = new RestRequest(ConfigSetting.api_prefix + matchDic[postData.GetType()].api, Method.POST);
                request.AddHeader("Content-Type", "application/json; charset=utf-8");
                request.AddJsonBody(postData);
                var response = client.Execute(request);
                result = json.Deserialize<ApiResponse>(response);
            }
            catch (Exception ex)
            {
                result = new ApiResponse();
                result.suc = 0;
                result.msg = ex.Message;
                result.dataMap = null;
            }
            return result;
        }

        public ApiResponse LoginApiFunc(string userId, string password, int usertype)
        {
            LoginAPI login = new LoginAPI();
            login.userID = userId;
            login.password = password;
            login.usertype = usertype;
            return PostQuery(login);
        }

        public ApiResponse FindTapApiFunc(int pub_id, string serial_number)
        {
            FindTapApi findTap = new FindTapApi();
            findTap.pub_id = pub_id;
            findTap.serial_number = serial_number;
            return PostQuery(findTap);
        }

        public ApiResponse GetTagInfoApiFunc(int pub_id, string serial_number, string tag_data)
        {
            GetTagInfoApi getTagInfo = new GetTagInfoApi();
            getTagInfo.pub_id = pub_id;
            getTagInfo.serial_number = serial_number;
            getTagInfo.tag_data = tag_data;
            return PostQuery(getTagInfo);
        }

        public ApiResponse GetTapProductInfoFunc(int pub_id, string serial_number)
        {
            GetTapProductInfoApi info = new GetTapProductInfoApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            return PostQuery(info);
        }

        public ApiResponse SetDeviceInfoFunc(int pub_id, string serial_number, int is_keg_reset, int is_valve_error, int is_soldout, int status, int is_valve_on)
        {
            SetDeviceInfoApi info = new SetDeviceInfoApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.is_keg_reset = is_keg_reset;
            info.is_valve_error = is_valve_error;
            info.is_soldout = is_soldout;
            info.status = status;
            info.is_valve_on = is_valve_on;
            return PostQuery(info);
        }

        public ApiResponse GetOrderNumberFunc(int pub_id, string serial_number, string tag_data)
        {
            GetOrderNumberApi info = new GetOrderNumberApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.tag_data = tag_data;
            return PostQuery(info);
        }

        public ApiResponse SetOrderNumberFunc(int pub_id, string serial_number, int last_order_number)
        {
            SetOrderNumberApi info = new SetOrderNumberApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.last_order_number = last_order_number;
            return PostQuery(info);
        }

        public ApiResponse CreateOrderFunc(int pub_id, string serial_number, int last_order_number, int product_id, string tag_data, int using_amount, int is_soldout)
        {
            CreateOrderApi info = new CreateOrderApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.order_number = last_order_number;
            info.product_id = product_id;
            info.tag_data = tag_data;
            info.using_amount = using_amount;
            info.is_soldout = is_soldout;
            return PostQuery(info);
        }

        public ApiResponse GetProductQuantityFunc(int pub_id, string serial_number)
        {
            GetProductQuantityApi info = new GetProductQuantityApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            return PostQuery(info);
        }

        public ApiResponse SetTapPulseFunc(int pub_id, string serial_number, int pulse)
        {
            SetTapPulseApi info = new SetTapPulseApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.pulse = pulse;
            return PostQuery(info);
        }

        public ApiResponse PosExeSuccessFunc(int pub_id)
        {
            PosExeSuccessApi info = new PosExeSuccessApi();
            info.pub_id = pub_id;
            return PostQuery(info);
        }

        public ApiResponse PosExeDisconnectFunc(int pub_id)
        {
            PosExeDisconnectApi info = new PosExeDisconnectApi();
            info.pub_id = pub_id;
            return PostQuery(info);
        }

        public ApiResponse SensorNotWorkFunc(int pub_id, string serial_number, string tag_data)
        {
            SensorNotWorkApi info = new SensorNotWorkApi();
            info.pub_id = pub_id;
            info.serial_number = serial_number;
            info.tag_data = tag_data;
            return PostQuery(info);
        }
    }
}
