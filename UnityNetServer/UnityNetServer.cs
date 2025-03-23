using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace wtesler.UnityNetServer {
  public class UnityNetServer : MonoBehaviour {
    public int port = 8080;

    private HttpListener httpListener;
    private CancellationTokenSource cancellationTokenSource;
    private bool isRunning;

    private readonly Dictionary<string, Dictionary<string, Controller>> controllerDict = new();
    private static readonly ConcurrentQueue<Func<Task>> mainThreadActions = new();

    private void Start() {
      foreach (var method in Enum.GetNames(typeof(Controller.Method))) {
        controllerDict[method] = new Dictionary<string, Controller>();
      }

      var controllers = GetComponentsInChildren<Controller>();
      foreach (var controller in controllers) {
        var methodStr = controller.GetMethod().ToString();
        var path = controller.GetPath();
        var methodDict = controllerDict[methodStr];
        if (methodDict.ContainsKey(path)) {
          throw new Exception($"{methodStr} | {path} already defined.");
        }
        methodDict[path] = controller;
      }

      StartServer();
    }

    private async void Update() {
      while (mainThreadActions.Count > 0) {
        if (mainThreadActions.TryDequeue(out var asyncAction)) {
          await asyncAction();
        }
      }
    }

    private void OnApplicationQuit() {
      StopServer();
    }

    private void OnDisable() {
      StopServer();
    }

    public static void EnqueueMainThread(Func<Task> asyncAction) {
      mainThreadActions.Enqueue(asyncAction);
    }

    public static void EnqueueActionMainThread(Action asyncAction) {
      mainThreadActions.Enqueue(() => Task.Run(asyncAction));
    }

    private void StartServer() {
      if (isRunning)
        return;

      isRunning = true;
      cancellationTokenSource = new CancellationTokenSource();
      Task.Run(() => RunServer(cancellationTokenSource.Token));
      Debug.Log($"Server started on port {port}");
    }

    private void StopServer() {
      if (!isRunning)
        return;

      isRunning = false;
      cancellationTokenSource.Cancel();
      httpListener?.Close();
      Debug.Log("Server stopped.");
    }

    private async Task RunServer(CancellationToken cancellationToken) {
      httpListener = new HttpListener();
      httpListener.Prefixes.Add($"http://*:{port}/");
      httpListener.Start();

      try {
        while (!cancellationToken.IsCancellationRequested) {
          HttpListenerContext context = await httpListener.GetContextAsync();
          if (cancellationToken.IsCancellationRequested) {
            break;
          }

          HttpListenerRequest request = context.Request;
          HttpListenerResponse response = context.Response;

          var method = request.HttpMethod;
          var path = request.Url.AbsolutePath;

          if (!controllerDict[method].ContainsKey(path)) {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentLength64 = 0;
            response.OutputStream.Close();
          } else {
            var controller = controllerDict[method][path];

            async Task HandleFunc() {
              await HandleRequest(controller, request, response, cancellationToken);
            }

            if (controller.RunOnMainThread()) {
              EnqueueMainThread(HandleFunc);
            } else {
              ThreadPool.QueueUserWorkItem(_ => HandleFunc().GetAwaiter().GetResult());
            }
          }
        }
      } catch (HttpListenerException ex) {
        if (!cancellationToken.IsCancellationRequested) {
          Debug.LogError($"Network Error: {ex.Message} {ex.StackTrace}");
        }
      } finally {
        httpListener?.Stop();
      }
    }

    private async Task HandleRequest(
      Controller controller, 
      HttpListenerRequest request, 
      HttpListenerResponse response,
      CancellationToken cancellationToken
    ) {
      try {
        if (!controller.IsInitialized()) {
          controller.InitializeInternal();
          controller.Initialize();
        }

        var controllerResponse = await controller.Handle(request);

        var responseType = controller.GetResponseType();

        string data = "";
        if (responseType == "application/json") {
          data = JsonUtility.ToJson(controllerResponse.data);
        } else if (responseType.StartsWith("text")) {
          data = (string)controllerResponse.data;
        }

        byte[] dataBuffer = Encoding.UTF8.GetBytes(data);

        response.StatusCode = controllerResponse.statusCode == 0 ? 200 : controllerResponse.statusCode;
        response.ContentType = responseType;
        response.ContentLength64 = dataBuffer.Length;

        await response.OutputStream.WriteAsync(dataBuffer, 0, dataBuffer.Length, cancellationToken);
        response.OutputStream.Close();
      } catch (Exception ex) {
        EnqueueActionMainThread(() => Debug.LogError($"Error handling request: {ex.Message} {ex.StackTrace}"));

        string responseJson = JsonUtility.ToJson(new ServerError {
          message = "Server Error"
        });

        byte[] buffer = Encoding.UTF8.GetBytes(responseJson);

        var statusCode = 500;
        if (ex.Data.Contains("statusCode")) {
          statusCode = (int)ex.Data["statusCode"];
        }

        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
        response.StatusCode = statusCode;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        response.OutputStream.Close();
      }
    }

    [Serializable]
    public class ServerError {
      public string message;
    }
  }
}
