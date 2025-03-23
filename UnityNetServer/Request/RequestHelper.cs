using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

namespace wtesler.UnityNetServer {
  public static class RequestHelper {
    public static async Task<T> ParseRequest<T>(HttpListenerRequest request, Controller.Method method) where T : new() {
      switch (method) {
        case Controller.Method.GET:
          return ParseGetRequest<T>(request);
        case Controller.Method.POST:
          return await ParsePostRequest<T>(request);
        case Controller.Method.PUT:
        case Controller.Method.DELETE:
        default:
          throw new Exception($"Unrecognized method: {method}");
      }
    }

    public static async Task<T> ParsePostRequest<T>(HttpListenerRequest request) {
      using StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
      string requestBody = await reader.ReadToEndAsync();
      return JsonUtility.FromJson<T>(requestBody);
    }

    public static T ParseGetRequest<T>(HttpListenerRequest request) where T : new() {
      var query = HttpUtility.ParseQueryString(request.Url.Query);
      var typedRequest = new T();
      var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var prop in properties) {
        string value = query[prop.Name];
        if (value != null) {
          object convertedValue = Convert.ChangeType(value, prop.PropertyType);
          prop.SetValue(typedRequest, convertedValue);
        }
      }

      return typedRequest;
    }
  }
}
